using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cadenza.Agent;

// Wire-format DTOs for OpenAI's Responses API (POST /v1/responses).
//
// The Responses API is what current OpenAI Codex CLI exclusively uses
// (Chat Completion support was removed Feb 2026). It differs from Chat
// Completion in several ways:
//
//   - Top-level field is `input` (array of items), not `messages`.
//   - Each item has a `type` discriminator: `message`, `function_call`,
//     `function_call_output`, `reasoning`, …
//   - Streaming emits ~15 distinct SSE event types (`response.created`,
//     `response.output_item.added`, `response.output_text.delta`, …) instead
//     of the simple `choices[0].delta` shape.
//   - Multi-turn state is tracked by `previous_response_id` (server-side).
//
// We implement the minimum surface Codex requires:
//   - `POST /v1/responses` (streaming and unary)
//   - in-memory `previous_response_id` chain bookkeeping
//   - passthrough for client-supplied tools (Codex sends its own `shell` /
//     `apply_patch` tool definitions — we never auto-invoke those)

internal sealed class ResponsesRequest
{
    [JsonPropertyName("model")] public string? Model { get; set; }
    [JsonPropertyName("input")] public JsonElement? Input { get; set; }
    [JsonPropertyName("instructions")] public string? Instructions { get; set; }
    [JsonPropertyName("tools")] public List<ResponsesTool>? Tools { get; set; }
    [JsonPropertyName("tool_choice")] public JsonElement? ToolChoice { get; set; }
    [JsonPropertyName("stream")] public bool? Stream { get; set; }
    [JsonPropertyName("temperature")] public double? Temperature { get; set; }
    [JsonPropertyName("top_p")] public double? TopP { get; set; }
    [JsonPropertyName("max_output_tokens")] public int? MaxOutputTokens { get; set; }
    [JsonPropertyName("previous_response_id")] public string? PreviousResponseId { get; set; }
    [JsonPropertyName("store")] public bool? Store { get; set; }
    [JsonPropertyName("reasoning")] public JsonElement? Reasoning { get; set; }
    [JsonPropertyName("metadata")] public JsonElement? Metadata { get; set; }
}

internal sealed class ResponsesTool
{
    [JsonPropertyName("type")] public string Type { get; set; } = "function";
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("parameters")] public JsonElement? Parameters { get; set; }
    [JsonPropertyName("strict")] public bool? Strict { get; set; }
    // Built-in tools (web_search_preview, file_search, …) carry extra
    // fields here. We deserialize them loosely and pass through to the
    // model when possible.
}

internal sealed class ResponseObject
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("object")] public string Object { get; set; } = "response";
    [JsonPropertyName("created_at")] public long CreatedAt { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "in_progress";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
    [JsonPropertyName("output")] public List<JsonNode> Output { get; set; } = new();
    [JsonPropertyName("usage")] public ResponsesUsage? Usage { get; set; }
    [JsonPropertyName("previous_response_id")] public string? PreviousResponseId { get; set; }
    [JsonPropertyName("metadata")] public JsonElement? Metadata { get; set; }
}

internal sealed class ResponsesUsage
{
    [JsonPropertyName("input_tokens")] public int InputTokens { get; set; }
    [JsonPropertyName("output_tokens")] public int OutputTokens { get; set; }
    [JsonPropertyName("total_tokens")] public int TotalTokens { get; set; }
}

// ─── SSE event envelopes ──────────────────────────────────────────────────
//
// We use one generic envelope per event family. The wire format demands a
// `type` discriminator and event-specific payload fields; rather than carve
// out 15 record types we keep the payload as a `JsonNode` and let the
// handler set whichever fields are required for each event.

internal sealed class ResponsesStreamEvent
{
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("sequence_number")] public int SequenceNumber { get; set; }

    // Common payload fields — only the relevant ones are non-null per event.
    [JsonPropertyName("response")] public ResponseObject? Response { get; set; }
    [JsonPropertyName("output_index")] public int? OutputIndex { get; set; }
    [JsonPropertyName("content_index")] public int? ContentIndex { get; set; }
    [JsonPropertyName("item_id")] public string? ItemId { get; set; }
    [JsonPropertyName("item")] public JsonNode? Item { get; set; }
    [JsonPropertyName("part")] public JsonNode? Part { get; set; }
    [JsonPropertyName("delta")] public string? Delta { get; set; }
    [JsonPropertyName("text")] public string? Text { get; set; }
    [JsonPropertyName("arguments")] public string? Arguments { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = false, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ResponsesRequest))]
[JsonSerializable(typeof(ResponseObject))]
[JsonSerializable(typeof(ResponsesStreamEvent))]
internal partial class OpenAiResponsesJsonCtx : JsonSerializerContext { }
