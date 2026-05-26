#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.14

// Codex CLI on top of any model OpenRouter serves.
//
// Codex CLI talks Responses API; we route those requests through
// OpenRouter's OpenAI-compatible Chat Completion endpoint and re-emit them
// as Responses SSE for Codex. The big win is model choice — point Codex
// at `anthropic/claude-3.5-sonnet`, `openai/gpt-5`, `meta-llama/llama-3.1-405b-instruct`,
// `google/gemini-2.0-flash-exp`, or any of the hundreds of OpenRouter models
// without Codex caring that it's not actually OpenAI.
//
// This sample auto-generates a sample-local Codex home directory so it
// never touches your real ~/.codex/. Drop into one terminal:
//
//   $env:OPENROUTER_API_KEY = "sk-or-v1-..."    # https://openrouter.ai/keys
//   $env:OPENROUTER_MODEL   = "anthropic/claude-3.5-sonnet"   # optional
//   dotnet run agent-codex-openrouter.cs
//
// The script writes `<script-dir>/.cadenza-codex/config.toml` and
// `cadenza-catalog.json` and prints the exact env-var to set in the second
// terminal:
//
//   $env:CODEX_HOME      = "...full path printed above..."
//   $env:CADENZA_API_KEY = "any-non-empty-string"
//   codex
//
// Codex reads `config.toml` from `CODEX_HOME` instead of `~/.codex/`, so
// your global config stays untouched. The catalog JSON registers the
// `cadenza-codex-openrouter` slug so Codex no longer prints "Defaulting to
// fallback metadata".
//
// NOTE: on the Responses path, Codex brings its own tools (`shell`,
// `apply_patch`, `update_plan`). Server-side Tool(...) registrations are
// intentionally NOT exposed here.
//
// Browse OpenRouter's model catalog: https://openrouter.ai/models

using System.ClientModel;
using OpenAI;

var apiKey = Env.Get("OPENROUTER_API_KEY")
    ?? throw new InvalidOperationException("OPENROUTER_API_KEY env var missing — get one at https://openrouter.ai/keys");
var model  = Env.Get("OPENROUTER_MODEL") ?? "anthropic/claude-3.5-sonnet";

ServedModelName = "cadenza-codex-openrouter";

// ─── Generate a sample-local Codex home directory ──────────────────────
//
// CODEX_HOME replaces ~/.codex/ entirely, so we ship a complete config.toml
// + catalog.json pair next to this script. The catalog entry's required
// fields come from codex-rs/protocol/src/openai_models.rs `ModelInfo` —
// every key below is mandatory; deserialization fails if any are dropped.

var codexHome = Path.Combine(Env.Cwd, ".cadenza-codex-openrouter");
MakeDir(codexHome);

var catalogPath = Path.Combine(codexHome, "cadenza-catalog.json").Replace('\\', '/');
var configToml = $"""
    model          = "cadenza-codex-openrouter"
    model_provider = "cadenza"
    model_catalog_json = "{catalogPath}"

    [model_providers.cadenza]
    name     = "Cadenza.Agent (OpenRouter-backed)"
    base_url = "http://localhost:8080/v1"
    wire_api = "responses"
    env_key  = "CADENZA_API_KEY"
    stream_idle_timeout_ms = 300000
    """;
WriteText(Path.Combine(codexHome, "config.toml"), configToml);

// 200K context covers Claude 3.5 Sonnet, GPT-4o, and most modern models on
// OpenRouter. If you point OPENROUTER_MODEL at something smaller (e.g.
// gpt-4o-mini at 128K), lower context_window / max_context_window to match
// — Codex truncates to this number, so over-declaring causes silent token
// overflows on the backing model.
var catalogJson = """
    {
      "models": [
        {
          "slug": "cadenza-codex-openrouter",
          "display_name": "Cadenza (OpenRouter)",
          "description": "OpenRouter-backed agent served by Cadenza.Agent",
          "supported_reasoning_levels": [],
          "shell_type": "default",
          "visibility": "list",
          "supported_in_api": true,
          "priority": 50,
          "availability_nux": null,
          "upgrade": null,
          "base_instructions": "",
          "supports_reasoning_summaries": false,
          "support_verbosity": false,
          "default_verbosity": null,
          "apply_patch_tool_type": "freeform",
          "truncation_policy": { "mode": "tokens", "limit": 8192 },
          "supports_parallel_tool_calls": true,
          "context_window": 200000,
          "max_context_window": 200000,
          "auto_compact_token_limit": 180000,
          "effective_context_window_percent": 95,
          "experimental_supported_tools": []
        }
      ]
    }
    """;
WriteText(Path.Combine(codexHome, "cadenza-catalog.json"), catalogJson);

WriteLine();
WriteLine($"Codex config generated at: {codexHome}");
WriteLine();
WriteLine("In another terminal, run:");
WriteLine();
WriteLine($"  $env:CODEX_HOME      = \"{codexHome}\"");
WriteLine($"  $env:CADENZA_API_KEY = \"any-non-empty-string\"");
WriteLine($"  codex");
WriteLine();

// ─── Hook up OpenRouter as the LLM backend ─────────────────────────────

var openAiOptions = new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") };
var chatClient    = new OpenAI.Chat.ChatClient(model, new ApiKeyCredential(apiKey), openAiOptions)
    .AsIChatClient();

UseChatClient(chatClient);

await Run();
