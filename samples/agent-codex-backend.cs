#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.14

// Codex CLI backend, Ollama edition.
//
// As of Feb 2026, Codex CLI speaks the OpenAI Responses API exclusively —
// Chat Completion support was removed (codex-rs: WireApi enum has only
// `Responses`). Cadenza.Agent ≥ 1.0.14 serves `POST /v1/responses` so
// Codex can connect directly without a proxy.
//
// This sample auto-generates a sample-local Codex home directory so it
// never touches your real ~/.codex/. Run in one terminal:
//
//   dotnet run agent-codex-backend.cs
//
// The script writes `<script-dir>/.cadenza-codex-backend/config.toml` and
// `cadenza-catalog.json` and prints the exact env-var to set in the second
// terminal:
//
//   $env:CODEX_HOME      = "...full path printed above..."
//   $env:CADENZA_API_KEY = "any-non-empty-string"
//   codex
//
// Codex reads `config.toml` from `CODEX_HOME` instead of `~/.codex/`, so
// your global config stays untouched. The catalog JSON registers the
// `cadenza-codex` slug so Codex no longer prints "Defaulting to fallback
// metadata".
//
// Behavior:
//   - We never auto-invoke tools — Codex owns its tools (shell, apply_patch,
//     update_plan). Server-side Tool(...) registrations are intentionally
//     not exposed on the Responses endpoint.
//   - previous_response_id is honored (multi-turn sessions work).
//   - The model below is what we forward Codex's prompts to. Swap freely.

ServedModelName = "cadenza-codex";
Port            = 8080;

// ─── Generate a sample-local Codex home directory ──────────────────────

var codexHome = Path.Combine(Env.Cwd, ".cadenza-codex-backend");
MakeDir(codexHome);

var catalogPath = Path.Combine(codexHome, "cadenza-catalog.json").Replace('\\', '/');
var configToml = $"""
    model          = "cadenza-codex"
    model_provider = "cadenza"
    model_catalog_json = "{catalogPath}"

    [model_providers.cadenza]
    name     = "Cadenza.Agent local (Ollama-backed)"
    base_url = "http://localhost:8080/v1"
    wire_api = "responses"
    env_key  = "CADENZA_API_KEY"
    stream_idle_timeout_ms = 300000
    """;
WriteText(Path.Combine(codexHome, "config.toml"), configToml);

// 32K context fits most coder-tuned Ollama models (qwen2.5-coder:7b,
// deepseek-coder-v2:lite, etc.). Raise to 128K if you swap in a larger
// long-context model — Codex truncates to this number, so over-declaring
// will silently overflow the backing model's actual window.
var catalogJson = """
    {
      "models": [
        {
          "slug": "cadenza-codex",
          "display_name": "Cadenza (Ollama)",
          "description": "Local Ollama-backed agent served by Cadenza.Agent",
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
          "truncation_policy": { "mode": "tokens", "limit": 4096 },
          "supports_parallel_tool_calls": false,
          "context_window": 32768,
          "max_context_window": 32768,
          "auto_compact_token_limit": 28000,
          "effective_context_window_percent": 90,
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

// ─── Hook up the LLM backend ───────────────────────────────────────────

UseOllama("qwen2.5-coder:7b");   // or UseOpenAi("gpt-4o-mini") / UseAnthropic("claude-3-5-sonnet-latest")

await Run();
