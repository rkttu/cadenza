using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;

namespace Cadenza.Agent;

// Handles POST /v1/responses (the OpenAI Responses API surface that Codex
// CLI now requires exclusively). We do enough to make Codex happy:
//
//   - Map the incoming `input[]` array to a flat List<ChatMessage>.
//   - Honor `previous_response_id` by replaying the cached history before
//     the new input.
//   - Stream `ChatResponseUpdate` instances out as the matching Responses
//     SSE event sequence (~response.created → output_item.added →
//     output_text.delta* → output_item.done → response.completed).
//   - Pass client-supplied tools (Codex's `shell`, `apply_patch`, …) to the
//     model as raw declarations — they are NEVER auto-invoked on the server
//     side. They stream back as `function_call` items so the client owns
//     execution.
//
// Server-side `Tool(...)` registrations are intentionally NOT exposed on
// this endpoint; they are a Chat-Completion-only feature. The reason: every
// Codex turn carries the client's own toolset, and we'd have to teach the
// MEAI middleware to selectively invoke just our subset — which it does
// not support cleanly. The cost is that Cadenza tools won't show up to
// Codex; the win is that the wire path stays simple and predictable.

internal static class AgentResponsesEndpoint
{
    private static readonly ConcurrentDictionary<string, List<ChatMessage>> _history = new();

    public static async Task HandleAsync(HttpRequest request, HttpResponse response, CancellationToken ct)
    {
        ResponsesRequest? req;
        try
        {
            req = await JsonSerializer.DeserializeAsync(
                request.Body,
                OpenAiResponsesJsonCtx.Default.ResponsesRequest,
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            await response.WriteAsync($"{{\"error\":{{\"message\":\"Malformed request: {ex.Message}\"}}}}", ct).ConfigureAwait(false);
            return;
        }

        if (req is null)
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
            await response.WriteAsync("{\"error\":{\"message\":\"empty body\"}}", ct).ConfigureAwait(false);
            return;
        }

        var messages = new List<ChatMessage>();

        if (!string.IsNullOrEmpty(req.PreviousResponseId)
            && _history.TryGetValue(req.PreviousResponseId!, out var prior))
        {
            messages.AddRange(prior);
        }
        else if (!string.IsNullOrEmpty(req.Instructions))
        {
            messages.Add(new ChatMessage(ChatRole.System, req.Instructions!));
        }
        else
        {
            messages.Add(new ChatMessage(ChatRole.System, Agent.CurrentSystemPrompt));
        }

        if (req.Input is JsonElement input)
            AppendInputToHistory(input, messages);

        var options = new ChatOptions
        {
            Temperature = req.Temperature is null ? null : (float)req.Temperature,
            TopP = req.TopP is null ? null : (float)req.TopP,
            MaxOutputTokens = req.MaxOutputTokens,
            Tools = req.Tools is { Count: > 0 } ? MapClientTools(req.Tools) : null,
        };

        var responseId = "resp_" + Guid.NewGuid().ToString("N");
        var stream     = req.Stream == true;

        if (stream)
            await StreamResponseAsync(messages, options, req, responseId, response, ct).ConfigureAwait(false);
        else
            await SendResponseAsync(messages, options, req, responseId, response, ct).ConfigureAwait(false);
    }

    // ─── Input mapping ────────────────────────────────────────────────────

    private static void AppendInputToHistory(JsonElement input, List<ChatMessage> messages)
    {
        // `input` can be either a plain string (rare) or an array of items.
        if (input.ValueKind == JsonValueKind.String)
        {
            messages.Add(new ChatMessage(ChatRole.User, input.GetString() ?? string.Empty));
            return;
        }

        if (input.ValueKind != JsonValueKind.Array) return;

        foreach (var item in input.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var type = item.TryGetProperty("type", out var t) ? t.GetString() : "message";

            switch (type)
            {
                case null:
                case "message":
                    messages.Add(MapInputMessage(item));
                    break;
                case "function_call":
                {
                    var name    = item.TryGetProperty("name",     out var n) ? n.GetString() ?? "" : "";
                    var callId  = item.TryGetProperty("call_id",  out var c) ? c.GetString() ?? "" : "";
                    var argsRaw = item.TryGetProperty("arguments", out var a) ? a.GetString() ?? "{}" : "{}";
                    Dictionary<string, object?>? args = null;
                    try { args = JsonSerializer.Deserialize<Dictionary<string, object?>>(argsRaw); }
                    catch { /* leave null on malformed */ }
                    messages.Add(new ChatMessage(ChatRole.Assistant,
                        new List<AIContent> { new FunctionCallContent(callId, name, args) }));
                    break;
                }
                case "function_call_output":
                {
                    var callId = item.TryGetProperty("call_id", out var c) ? c.GetString() ?? "" : "";
                    var output = item.TryGetProperty("output",  out var o) ? o.GetString() ?? "" : "";
                    messages.Add(new ChatMessage(ChatRole.Tool,
                        new List<AIContent> { new FunctionResultContent(callId, output) }));
                    break;
                }
                case "reasoning":
                    // Skip — we don't surface reasoning back into history.
                    break;
            }
        }
    }

    private static ChatMessage MapInputMessage(JsonElement item)
    {
        var role = item.TryGetProperty("role", out var r) ? r.GetString() ?? "user" : "user";
        var chatRole = role switch
        {
            "system"    => ChatRole.System,
            "developer" => ChatRole.System,
            "assistant" => ChatRole.Assistant,
            "tool"      => ChatRole.Tool,
            _           => ChatRole.User,
        };

        var sb = new StringBuilder();
        if (item.TryGetProperty("content", out var content))
        {
            if (content.ValueKind == JsonValueKind.String)
                sb.Append(content.GetString());
            else if (content.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in content.EnumerateArray())
                {
                    if (part.ValueKind != JsonValueKind.Object) continue;
                    if (part.TryGetProperty("type", out var pt))
                    {
                        var ptype = pt.GetString();
                        if (ptype is "input_text" or "output_text" or "text"
                            && part.TryGetProperty("text", out var pText))
                            sb.Append(pText.GetString());
                    }
                }
            }
        }

        return new ChatMessage(chatRole, sb.ToString());
    }

    private static IList<AITool> MapClientTools(List<ResponsesTool> tools)
    {
        // Codex's tool definitions arrive as `{type: "function", name, description, parameters}`
        // or (rarely) `{type: "function", function: {…}}`. We declare them as schema-only
        // AIFunction instances — `PassthroughFunction` returns the schema but its
        // InvokeAsync throws, ensuring `FunctionInvokingChatClient` never picks them up
        // for auto-invocation. (We use `Agent.RawChatClient` for Responses anyway, so
        // the middleware isn't in the chain — this is a belt-and-suspenders measure.)
        var result = new List<AITool>(tools.Count);
        foreach (var t in tools)
        {
            if (t.Type != "function") continue;
            var name = t.Name;
            if (string.IsNullOrEmpty(name)) continue;
            var schema = t.Parameters?.ValueKind == JsonValueKind.Object
                ? JsonNode.Parse(t.Parameters.Value.GetRawText())
                : null;
            result.Add(new PassthroughFunction(name!, t.Description ?? "", schema));
        }
        return result;
    }

    // ─── Streaming path ────────────────────────────────────────────────────

    private static async Task StreamResponseAsync(
        List<ChatMessage> messages, ChatOptions options, ResponsesRequest req,
        string responseId, HttpResponse response, CancellationToken ct)
    {
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers.Connection   = "keep-alive";
        response.Headers["X-Accel-Buffering"] = "no";

        var createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var modelName = req.Model ?? Agent.ServedModelName;
        var seq       = 0;

        var initial = new ResponseObject
        {
            Id        = responseId,
            CreatedAt = createdAt,
            Status    = "in_progress",
            Model     = modelName,
            PreviousResponseId = req.PreviousResponseId,
        };
        await EmitAsync(response, new ResponsesStreamEvent
        {
            Type = "response.created", SequenceNumber = seq++, Response = initial
        }, ct).ConfigureAwait(false);
        await EmitAsync(response, new ResponsesStreamEvent
        {
            Type = "response.in_progress", SequenceNumber = seq++, Response = initial
        }, ct).ConfigureAwait(false);

        // Track a single text output item plus zero-or-more function_call items.
        var outputIndex     = 0;
        var textItemOpen    = false;
        var textItemId      = "";
        var textBuffer      = new StringBuilder();
        var functionCalls   = new List<(string ItemId, string CallId, string Name, StringBuilder Args)>();
        var assistantBuffer = new StringBuilder();

        await foreach (var update in Agent.RawChatClient
            .GetStreamingResponseAsync(messages, options, ct)
            .ConfigureAwait(false))
        {
            foreach (var contentItem in update.Contents)
            {
                switch (contentItem)
                {
                    case TextContent tc when !string.IsNullOrEmpty(tc.Text):
                    {
                        if (!textItemOpen)
                        {
                            textItemId = "msg_" + Guid.NewGuid().ToString("N");
                            await EmitAsync(response, new ResponsesStreamEvent
                            {
                                Type           = "response.output_item.added",
                                SequenceNumber = seq++,
                                OutputIndex    = outputIndex,
                                Item           = BuildAssistantMessageItemNode(textItemId, ""),
                            }, ct).ConfigureAwait(false);
                            await EmitAsync(response, new ResponsesStreamEvent
                            {
                                Type           = "response.content_part.added",
                                SequenceNumber = seq++,
                                ItemId         = textItemId,
                                OutputIndex    = outputIndex,
                                ContentIndex   = 0,
                                Part           = new JsonObject { ["type"] = "output_text", ["text"] = "" },
                            }, ct).ConfigureAwait(false);
                            textItemOpen = true;
                        }
                        textBuffer.Append(tc.Text);
                        assistantBuffer.Append(tc.Text);
                        await EmitAsync(response, new ResponsesStreamEvent
                        {
                            Type           = "response.output_text.delta",
                            SequenceNumber = seq++,
                            ItemId         = textItemId,
                            OutputIndex    = outputIndex,
                            ContentIndex   = 0,
                            Delta          = tc.Text,
                        }, ct).ConfigureAwait(false);
                        break;
                    }
                    case FunctionCallContent fc:
                    {
                        // Close the current text item before opening a function_call item.
                        if (textItemOpen)
                        {
                            await CloseTextItemAsync(response, textItemId, outputIndex, textBuffer.ToString(), seq, ct).ConfigureAwait(false);
                            seq += 3;
                            outputIndex++;
                            textItemOpen = false;
                            textBuffer.Clear();
                        }

                        var fcItemId = "fc_" + Guid.NewGuid().ToString("N");
                        var argsJson = fc.Arguments is null
                            ? "{}"
                            : JsonSerializer.Serialize(fc.Arguments);
                        functionCalls.Add((fcItemId, fc.CallId, fc.Name, new StringBuilder(argsJson)));

                        await EmitAsync(response, new ResponsesStreamEvent
                        {
                            Type           = "response.output_item.added",
                            SequenceNumber = seq++,
                            OutputIndex    = outputIndex,
                            Item           = BuildFunctionCallItemNode(fcItemId, fc.CallId, fc.Name, ""),
                        }, ct).ConfigureAwait(false);
                        await EmitAsync(response, new ResponsesStreamEvent
                        {
                            Type           = "response.function_call_arguments.delta",
                            SequenceNumber = seq++,
                            ItemId         = fcItemId,
                            OutputIndex    = outputIndex,
                            Delta          = argsJson,
                        }, ct).ConfigureAwait(false);
                        await EmitAsync(response, new ResponsesStreamEvent
                        {
                            Type           = "response.function_call_arguments.done",
                            SequenceNumber = seq++,
                            ItemId         = fcItemId,
                            OutputIndex    = outputIndex,
                            Arguments      = argsJson,
                        }, ct).ConfigureAwait(false);
                        await EmitAsync(response, new ResponsesStreamEvent
                        {
                            Type           = "response.output_item.done",
                            SequenceNumber = seq++,
                            OutputIndex    = outputIndex,
                            Item           = BuildFunctionCallItemNode(fcItemId, fc.CallId, fc.Name, argsJson),
                        }, ct).ConfigureAwait(false);
                        outputIndex++;
                        break;
                    }
                }
            }
        }

        if (textItemOpen)
        {
            await CloseTextItemAsync(response, textItemId, outputIndex, textBuffer.ToString(), seq, ct).ConfigureAwait(false);
            seq += 3;
        }

        var finalOutput = new List<JsonNode>();
        if (assistantBuffer.Length > 0)
            finalOutput.Add(BuildAssistantMessageItemNode(textItemId, assistantBuffer.ToString())!);
        foreach (var (id, callId, name, args) in functionCalls)
            finalOutput.Add(BuildFunctionCallItemNode(id, callId, name, args.ToString())!);

        var final = new ResponseObject
        {
            Id        = responseId,
            CreatedAt = createdAt,
            Status    = "completed",
            Model     = modelName,
            Output    = finalOutput,
            PreviousResponseId = req.PreviousResponseId,
        };
        await EmitAsync(response, new ResponsesStreamEvent
        {
            Type = "response.completed", SequenceNumber = seq++, Response = final
        }, ct).ConfigureAwait(false);

        await response.Body.FlushAsync(ct).ConfigureAwait(false);

        if (req.Store != false)
            StoreHistory(responseId, messages, assistantBuffer.ToString(), functionCalls);
    }

    private static async Task CloseTextItemAsync(
        HttpResponse response, string itemId, int outputIndex, string text, int seq, CancellationToken ct)
    {
        await EmitAsync(response, new ResponsesStreamEvent
        {
            Type = "response.output_text.done", SequenceNumber = seq,
            ItemId = itemId, OutputIndex = outputIndex, ContentIndex = 0, Text = text,
        }, ct).ConfigureAwait(false);
        await EmitAsync(response, new ResponsesStreamEvent
        {
            Type = "response.content_part.done", SequenceNumber = seq + 1,
            ItemId = itemId, OutputIndex = outputIndex, ContentIndex = 0,
            Part = new JsonObject { ["type"] = "output_text", ["text"] = text },
        }, ct).ConfigureAwait(false);
        await EmitAsync(response, new ResponsesStreamEvent
        {
            Type = "response.output_item.done", SequenceNumber = seq + 2,
            OutputIndex = outputIndex,
            Item = BuildAssistantMessageItemNode(itemId, text),
        }, ct).ConfigureAwait(false);
    }

    private static JsonNode BuildAssistantMessageItemNode(string id, string text) => new JsonObject
    {
        ["id"]     = id,
        ["type"]   = "message",
        ["status"] = "completed",
        ["role"]   = "assistant",
        ["content"] = new JsonArray
        {
            new JsonObject
            {
                ["type"]        = "output_text",
                ["text"]        = text,
                ["annotations"] = new JsonArray(),
            },
        },
    };

    private static JsonNode BuildFunctionCallItemNode(string id, string callId, string name, string arguments) => new JsonObject
    {
        ["id"]        = id,
        ["type"]      = "function_call",
        ["status"]    = "completed",
        ["call_id"]   = callId,
        ["name"]      = name,
        ["arguments"] = arguments,
    };

    private static async Task EmitAsync(HttpResponse response, ResponsesStreamEvent evt, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(evt, OpenAiResponsesJsonCtx.Default.ResponsesStreamEvent);
        var sb   = new StringBuilder(json.Length + 32)
            .Append("event: ").Append(evt.Type).Append('\n')
            .Append("data: ").Append(json).Append("\n\n");
        await response.WriteAsync(sb.ToString(), ct).ConfigureAwait(false);
        await response.Body.FlushAsync(ct).ConfigureAwait(false);
    }

    // ─── Unary path ────────────────────────────────────────────────────────

    private static async Task SendResponseAsync(
        List<ChatMessage> messages, ChatOptions options, ResponsesRequest req,
        string responseId, HttpResponse response, CancellationToken ct)
    {
        var chatResponse = await Agent.RawChatClient.GetResponseAsync(messages, options, ct).ConfigureAwait(false);

        var output = new List<JsonNode>();
        var assistantText = new StringBuilder();
        var functionCalls = new List<(string ItemId, string CallId, string Name, StringBuilder Args)>();

        foreach (var msg in chatResponse.Messages)
        {
            foreach (var contentItem in msg.Contents)
            {
                if (contentItem is TextContent tc && !string.IsNullOrEmpty(tc.Text))
                    assistantText.Append(tc.Text);
                else if (contentItem is FunctionCallContent fc)
                {
                    var argsJson = fc.Arguments is null ? "{}" : JsonSerializer.Serialize(fc.Arguments);
                    var fcId     = "fc_" + Guid.NewGuid().ToString("N");
                    functionCalls.Add((fcId, fc.CallId, fc.Name, new StringBuilder(argsJson)));
                }
            }
        }

        var assistantMsgId = "msg_" + Guid.NewGuid().ToString("N");
        if (assistantText.Length > 0)
            output.Add(BuildAssistantMessageItemNode(assistantMsgId, assistantText.ToString()));
        foreach (var (id, callId, name, args) in functionCalls)
            output.Add(BuildFunctionCallItemNode(id, callId, name, args.ToString()));

        var payload = new ResponseObject
        {
            Id        = responseId,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Status    = "completed",
            Model     = req.Model ?? Agent.ServedModelName,
            Output    = output,
            PreviousResponseId = req.PreviousResponseId,
            Usage     = chatResponse.Usage is null ? null : new ResponsesUsage
            {
                InputTokens  = (int)(chatResponse.Usage.InputTokenCount  ?? 0),
                OutputTokens = (int)(chatResponse.Usage.OutputTokenCount ?? 0),
                TotalTokens  = (int)(chatResponse.Usage.TotalTokenCount  ?? 0),
            },
        };

        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(
            response.Body, payload, OpenAiResponsesJsonCtx.Default.ResponseObject, ct).ConfigureAwait(false);

        if (req.Store != false)
            StoreHistory(responseId, messages, assistantText.ToString(), functionCalls);
    }

    // ─── State store ──────────────────────────────────────────────────────

    private static void StoreHistory(
        string responseId, List<ChatMessage> priorMessages, string assistantText,
        List<(string ItemId, string CallId, string Name, StringBuilder Args)> functionCalls)
    {
        var snapshot = new List<ChatMessage>(priorMessages);
        if (!string.IsNullOrEmpty(assistantText))
            snapshot.Add(new ChatMessage(ChatRole.Assistant, assistantText));
        foreach (var (_, callId, name, args) in functionCalls)
        {
            snapshot.Add(new ChatMessage(ChatRole.Assistant,
                new List<AIContent>
                {
                    new FunctionCallContent(callId, name,
                        TryParseArgs(args.ToString())),
                }));
        }

        _history[responseId] = snapshot;

        // Trim — keep at most 256 active chains to bound memory. LRU-ish:
        // when we exceed, drop arbitrary entries.
        if (_history.Count > 256)
        {
            var toRemove = _history.Keys.Take(_history.Count - 256).ToList();
            foreach (var k in toRemove) _history.TryRemove(k, out _);
        }
    }

    private static Dictionary<string, object?>? TryParseArgs(string json)
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, object?>>(json); }
        catch { return null; }
    }
}

// AIFunction subclass used purely to declare a tool schema to the model
// without ever being invoked. The Responses path uses Agent.RawChatClient
// (no UseFunctionInvocation) so this is mainly defensive — if anything
// upstream tries to call us, the throw makes the bug obvious.

internal sealed class PassthroughFunction : AIFunction
{
    private readonly string _name;
    private readonly string _description;
    private readonly JsonElement _schema;

    public PassthroughFunction(string name, string description, JsonNode? schema)
    {
        _name        = name;
        _description = description;
        _schema      = schema is null
            ? JsonSerializer.SerializeToElement(new { type = "object", properties = new { } })
            : JsonSerializer.SerializeToElement(schema);
    }

    public override string Name        => _name;
    public override string Description => _description;
    public override JsonElement JsonSchema => _schema;

    protected override ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments, CancellationToken cancellationToken) =>
        throw new InvalidOperationException(
            $"PassthroughFunction '{_name}' is declaration-only and must not be invoked server-side.");
}
