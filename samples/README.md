# Cadenza Samples

> Read this in [한국어](README.ko.md).

Each `.cs` file is a self-contained file-based program — run it with:

```bash
dotnet run <file>.cs
```

Pin the SDK version in the `#:sdk` line of each sample to the latest published
release (the files in this folder currently pin `Cadenza@1.0.14`,
`Cadenza.Worker@1.0.14`, `Cadenza.Web@1.0.14`, `Cadenza.Mcp@1.0.14`,
`Cadenza.Agent@1.0.14`). MSBuild SDK references require an exact version —
see [docs/troubleshooting.md](../docs/troubleshooting.md) for details.

## Console scripts (`#:sdk Cadenza@...`)

| Sample | Demonstrates |
| --- | --- |
| [`console-hello.cs`](console-hello.cs) | Minimal — `Glob`, `ReadText`, `WriteLine` |
| [`console-count-files.cs`](console-count-files.cs) | Recursive `Glob` + LINQ grouping |
| [`console-deploy-guard.cs`](console-deploy-guard.cs) | `Capture` for git state, `Run` for build steps, `Env.Exit` for failure paths |
| [`console-http-fetch.cs`](console-http-fetch.cs) | `Http.GetJson` with a source-generated `JsonSerializerContext` (AOT-clean) |
| [`console-prompt-setup.cs`](console-prompt-setup.cs) | All four `Prompt.*` helpers, with CI fallback envelope |

## Worker scripts (`#:sdk Cadenza.Worker@...`)

| Sample | Demonstrates |
| --- | --- |
| [`worker-heartbeat.cs`](worker-heartbeat.cs) | Minimal periodic loop with graceful shutdown |
| [`worker-polling.cs`](worker-polling.cs) | `Worker.Config<T>` for typed config, periodic HTTP probe, `Log.Info/Warn/Debug` |

## Web scripts (`#:sdk Cadenza.Web@...`)

| Sample | Demonstrates |
| --- | --- |
| [`web-minimal.cs`](web-minimal.cs) | Hello + health + echo with Minimal API record binding |
| [`web-todo-api.cs`](web-todo-api.cs) | Full CRUD over an in-memory store using `Get`/`Post`/`Put`/`Delete` |

## MCP server scripts (`#:sdk Cadenza.Mcp@...`)

| Sample | Demonstrates |
| --- | --- |
| [`mcp-files.cs`](mcp-files.cs) | Minimal MCP server — `Tool` registration + `Run` for stdio transport |
| [`mcp-extended.cs`](mcp-extended.cs) | Full primitive set — `Tool` (external API), `Resource` (fixed URI), `Prompt` (template), `Log.Info` (stderr) |

Register a Cadenza.Mcp server with Claude Desktop by adding to its config:

```json
{
  "mcpServers": {
    "cadenza-files": {
      "command": "dotnet",
      "args": ["run", "/absolute/path/to/mcp-files.cs"]
    }
  }
}
```

**Important**: Cadenza.Mcp intentionally does not expose `WriteLine` / `Write` /
`ReadLine` as Tier 1 bare names. Stdio MCP servers carry JSON-RPC over stdout;
any stray text breaks the client connection. Use `Log.*` for diagnostics —
they route through `ILogger` to stderr.

## AI agent scripts (`#:sdk Cadenza.Agent@...`)

| Sample | Demonstrates |
| --- | --- |
| [`agent-basic.cs`](agent-basic.cs) | Minimal agent — `Tool` registrations + `UseOllama` + `Run` (OpenAI-compatible HTTP server on `localhost:8080`) |
| [`agent-rag-folder.cs`](agent-rag-folder.cs) | Tiny RAG-over-a-folder pattern — `search_docs` / `read_doc` tools the model decides when to call |
| [`agent-codex-backend.cs`](agent-codex-backend.cs) | Codex CLI backend with `wire_api = "responses"` config snippet — Ollama as the model |
| [`agent-codex-openrouter.cs`](agent-codex-openrouter.cs) | Codex CLI on top of any [OpenRouter](https://openrouter.ai/) model (Claude / GPT / Gemini / Llama / …) — proxy through Cadenza |
| [`agent-multi-llm.cs`](agent-multi-llm.cs) | Pick `Ollama` / `OpenAI` / `Anthropic` / `Azure OpenAI` at startup from `LLM_BACKEND` env var |
| [`agent-openrouter.cs`](agent-openrouter.cs) | Route through [OpenRouter](https://openrouter.ai/) (one API key, hundreds of models) via `UseChatClient` + custom OpenAI endpoint |
| [`agent-console-repl.cs`](agent-console-repl.cs) | `ChatLoop()` instead of `Run()` — interactive console, no HTTP server |

Point any OpenAI-compatible client at a `Cadenza.Agent`:

```bash
export OPENAI_BASE_URL=http://localhost:8080/v1
export OPENAI_API_KEY=any-non-empty-string
codex      # or aider, continue, cursor, sgpt, …
```

## Publishing as a single binary

Any of these samples can be turned into a self-contained binary:

```bash
dotnet publish console-deploy-guard.cs -r linux-x64 -c Release
```

See [docs/publishing-single-binary.md](../docs/publishing-single-binary.md)
for the full distribution matrix (compression, AOT opt-in, container packaging).
