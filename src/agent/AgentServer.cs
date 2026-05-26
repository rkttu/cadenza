using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Cadenza.Agent;

internal static class AgentServer
{
    public static async Task RunAsync()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.Urls.Add($"http://{Agent.HostName}:{Agent.Port}");

        app.MapGet("/v1/models", () => Results.Json(
            new ModelsListResponse
            {
                Data = new List<ModelInfo>
                {
                    new() { Id = Agent.ServedModelName, Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                },
            },
            OpenAiWireJsonCtx.Default.ModelsListResponse));

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        app.MapPost("/v1/chat/completions", async (HttpRequest request, HttpResponse response, CancellationToken ct) =>
        {
            ChatCompletionRequest? req;
            try
            {
                req = await JsonSerializer.DeserializeAsync(
                    request.Body,
                    OpenAiWireJsonCtx.Default.ChatCompletionRequest,
                    ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                await response.WriteAsync($"{{\"error\":{{\"message\":\"Malformed request: {ex.Message}\"}}}}", ct).ConfigureAwait(false);
                return;
            }

            if (req is null || req.Messages.Count == 0)
            {
                response.StatusCode = StatusCodes.Status400BadRequest;
                await response.WriteAsync("{\"error\":{\"message\":\"messages must contain at least one entry\"}}", ct).ConfigureAwait(false);
                return;
            }

            var messages = MapToMeai(req.Messages);
            var options = new ChatOptions
            {
                Tools = Agent.Tools,
                Temperature = req.Temperature is null ? null : (float)req.Temperature,
                TopP = req.TopP is null ? null : (float)req.TopP,
                MaxOutputTokens = req.MaxTokens,
            };

            if (req.Stream == true)
                await HandleStreamingAsync(messages, options, response, ct).ConfigureAwait(false);
            else
                await HandleSingleAsync(messages, options, response, ct).ConfigureAwait(false);
        });

        app.MapPost("/v1/responses", AgentResponsesEndpoint.HandleAsync);

        await app.RunAsync().ConfigureAwait(false);
    }

    private static async Task HandleSingleAsync(
        List<ChatMessage> messages, ChatOptions options, HttpResponse response, CancellationToken ct)
    {
        var meaiResponse = await Agent.ChatClient.GetResponseAsync(messages, options, ct).ConfigureAwait(false);
        var text = meaiResponse.Text ?? string.Empty;

        var payload = new ChatCompletionResponse
        {
            Id = "chatcmpl-" + Guid.NewGuid().ToString("N"),
            Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Model = Agent.ServedModelName,
            Choices = new List<ChatCompletionChoice>
            {
                new()
                {
                    Index = 0,
                    Message = new OpenAiMessage
                    {
                        Role = "assistant",
                        Content = JsonDocument.Parse(JsonSerializer.Serialize(text)).RootElement,
                    },
                    FinishReason = "stop",
                },
            },
            Usage = ExtractUsage(meaiResponse),
        };

        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(
            response.Body, payload, OpenAiWireJsonCtx.Default.ChatCompletionResponse, ct).ConfigureAwait(false);
    }

    private static async Task HandleStreamingAsync(
        List<ChatMessage> messages, ChatOptions options, HttpResponse response, CancellationToken ct)
    {
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers.Connection = "keep-alive";
        response.Headers["X-Accel-Buffering"] = "no";

        var id = "chatcmpl-" + Guid.NewGuid().ToString("N");
        var created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var first = true;

        await foreach (var update in Agent.ChatClient.GetStreamingResponseAsync(messages, options, ct).ConfigureAwait(false))
        {
            var text = update.Text;
            if (string.IsNullOrEmpty(text) && !first) continue;

            var chunk = new ChatCompletionChunk
            {
                Id = id,
                Created = created,
                Model = Agent.ServedModelName,
                Choices = new List<ChunkChoice>
                {
                    new()
                    {
                        Index = 0,
                        Delta = new ChunkDelta
                        {
                            Role = first ? "assistant" : null,
                            Content = text,
                        },
                        FinishReason = null,
                    },
                },
            };
            first = false;
            await WriteSseAsync(response, chunk, ct).ConfigureAwait(false);
        }

        // final chunk with finish_reason
        var done = new ChatCompletionChunk
        {
            Id = id,
            Created = created,
            Model = Agent.ServedModelName,
            Choices = new List<ChunkChoice>
            {
                new()
                {
                    Index = 0,
                    Delta = new ChunkDelta(),
                    FinishReason = "stop",
                },
            },
        };
        await WriteSseAsync(response, done, ct).ConfigureAwait(false);

        await response.WriteAsync("data: [DONE]\n\n", ct).ConfigureAwait(false);
        await response.Body.FlushAsync(ct).ConfigureAwait(false);
    }

    private static async Task WriteSseAsync(HttpResponse response, ChatCompletionChunk chunk, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(chunk, OpenAiWireJsonCtx.Default.ChatCompletionChunk);
        var sb = new StringBuilder(json.Length + 16).Append("data: ").Append(json).Append("\n\n");
        await response.WriteAsync(sb.ToString(), ct).ConfigureAwait(false);
        await response.Body.FlushAsync(ct).ConfigureAwait(false);
    }

    // ─── OpenAI ↔ MEAI message mapping ────────────────────────────────────

    private static List<ChatMessage> MapToMeai(IList<OpenAiMessage> messages)
    {
        var result = new List<ChatMessage>(messages.Count);
        foreach (var m in messages)
        {
            var role = m.Role.ToLowerInvariant() switch
            {
                "system" => ChatRole.System,
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                "tool" => ChatRole.Tool,
                _ => ChatRole.User,
            };

            var contents = new List<AIContent>();
            var text = ExtractText(m.Content);
            if (!string.IsNullOrEmpty(text))
                contents.Add(new TextContent(text));

            if (m.ToolCalls is { Count: > 0 })
            {
                foreach (var call in m.ToolCalls)
                {
                    Dictionary<string, object?>? args = null;
                    if (!string.IsNullOrEmpty(call.Function.Arguments))
                    {
                        try
                        {
                            args = JsonSerializer.Deserialize<Dictionary<string, object?>>(call.Function.Arguments);
                        }
                        catch { /* leave args null on malformed JSON */ }
                    }
                    contents.Add(new FunctionCallContent(call.Id, call.Function.Name, args));
                }
            }

            if (role == ChatRole.Tool && !string.IsNullOrEmpty(m.ToolCallId))
            {
                contents.Clear();
                contents.Add(new FunctionResultContent(m.ToolCallId!, text));
            }

            result.Add(new ChatMessage(role, contents));
        }
        return result;
    }

    private static string ExtractText(JsonElement? content)
    {
        if (content is null) return string.Empty;
        var element = content.Value;
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Concat(
                element.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.Object && e.TryGetProperty("type", out var t) && t.GetString() == "text")
                    .Select(e => e.TryGetProperty("text", out var txt) ? txt.GetString() ?? "" : "")),
            _ => string.Empty,
        };
    }

    private static ChatCompletionUsage? ExtractUsage(ChatResponse response)
    {
        var usage = response.Usage;
        if (usage is null) return null;
        return new ChatCompletionUsage
        {
            PromptTokens = (int)(usage.InputTokenCount ?? 0),
            CompletionTokens = (int)(usage.OutputTokenCount ?? 0),
            TotalTokens = (int)(usage.TotalTokenCount ?? 0),
        };
    }
}
