#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.13

// Codex CLI backend in ~30 lines.
//
// As of Feb 2026, Codex CLI speaks the OpenAI Responses API exclusively —
// Chat Completion support was removed (codex-rs: WireApi enum has only
// `Responses`). Cadenza.Agent ≥ 1.0.13 serves `POST /v1/responses` so
// Codex can connect directly without a proxy.
//
// IMPORTANT: this endpoint is PASSTHROUGH for client-supplied tools. Codex
// sends its own `shell` / `apply_patch` / `update_plan` tool definitions in
// every request; we forward them to the model verbatim and stream
// `function_call` items back so Codex executes them locally. Server-side
// `Tool(...)` registrations you might add here are intentionally NOT
// exposed on the Responses endpoint — they're a Chat-Completion-only
// feature today. Use this script as a "model adapter": pick the LLM and
// let Codex bring the toolset.
//
// Config: add this to ~/.codex/config.toml (or %USERPROFILE%\.codex\config.toml):
//
//     model_provider = "cadenza"
//     model          = "cadenza-codex"
//
//     [model_providers.cadenza]
//     name     = "Cadenza.Agent local"
//     base_url = "http://localhost:8080/v1"
//     wire_api = "responses"
//     env_key  = "CADENZA_API_KEY"     # any non-empty value works
//     stream_idle_timeout_ms = 300000
//
// Then export a placeholder key and run codex:
//
//     $env:CADENZA_API_KEY = "any-non-empty-string"
//     dotnet run agent-codex-backend.cs   # starts the server
//     codex                               # in another terminal
//
// Behavior:
//   - We never auto-invoke tools — Codex owns its tools (shell, apply_patch).
//   - previous_response_id is honored (multi-turn sessions work).
//   - The model below is what we forward Codex's prompts to. Swap freely.

ServedModelName = "cadenza-codex";   // shown in Codex's model picker
Port            = 8080;

UseOllama("qwen2.5-coder:7b");        // or UseOpenAi("gpt-4o-mini") / UseAnthropic("claude-3-5-sonnet-latest")

await Run();
