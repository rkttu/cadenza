# Cadenza.Agent

> Read this in [한국어](README.ko.md).

`Cadenza.Agent` is the AI-agent variant of the Cadenza SDK family — a single-file scripting MSBuild SDK that wraps [`Microsoft.Extensions.AI`](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai) (the vendor-neutral .NET LLM abstraction) and exposes an **OpenAI-compatible Chat Completion HTTP endpoint** out of the box. That means tools like Codex, Aider, Continue, and Cursor can talk to your agent as if it were OpenAI — just point them at `http://localhost:8080/v1`.

## Quick start

Create an `agent.cs` file:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.12

SystemPrompt("You are a helpful assistant with filesystem access.");

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern",
    (string pattern) => Glob(pattern).ToArray());

UseOllama("llama3.2");      // or UseOpenAi("gpt-4o-mini")
                            //    UseAnthropic("claude-3-5-sonnet-latest")
                            //    UseAzureOpenAi(endpoint, deployment)

await Run();                // boots the OpenAI-compatible HTTP server
                            // on http://localhost:8080
```

Run iteratively:

```bash
dotnet run agent.cs
```

Point any OpenAI-compatible client at it:

```bash
export OPENAI_BASE_URL=http://localhost:8080/v1
export OPENAI_API_KEY=any-non-empty-string
codex      # or aider, continue, cursor, sgpt, …
```

Or use the REPL instead of the HTTP server:

```csharp
UseOllama("llama3.2");
await ChatLoop();           // interactive console — no HTTP server
```

Publish as a self-contained single binary:

```bash
dotnet publish agent.cs -r linux-x64 -c Release
```

## What you get

| Helper | Purpose |
|---|---|
| `Tool(name, description, delegate)` | Register a callable tool. Parameter names/types become the JSON schema; the return value is the tool result. |
| `SystemPrompt(text)` | Override the default system prompt. |
| `UseOllama(model, baseUrl?)` | Use a local [Ollama](https://ollama.com) daemon as the LLM. |
| `UseOpenAi(model, apiKey?)` | Use OpenAI. Falls back to `OPENAI_API_KEY` env var. |
| `UseAnthropic(model, apiKey?)` | Use Anthropic via its OpenAI-compatible endpoint. Falls back to `ANTHROPIC_API_KEY`. |
| `UseAzureOpenAi(endpoint, deployment, apiKey?)` | Use Azure OpenAI. Falls back to `AZURE_OPENAI_API_KEY`. |
| `UseChatClient(IChatClient)` | Plug in any custom `IChatClient` — gets `UseFunctionInvocation()` wired automatically. |
| `Run()` | Start the OpenAI-compatible HTTP server (default `localhost:8080`). |
| `ChatLoop()` | Start an interactive console REPL. |
| `Reply(prompt)` | One-shot non-interactive call. Returns the assistant's text. |
| `Port`, `HostName`, `ServedModelName` | Configure the HTTP surface before calling `Run()`. |

## Endpoints exposed by `Run()`

- `POST /v1/chat/completions` — full OpenAI Chat Completion shape; supports `stream=true` (SSE).
- `GET  /v1/models` — single-entry list with `ServedModelName` as the id.
- `GET  /health` — liveness probe.

## Why an HTTP frontend?

Most existing coding agents and chat UIs already speak the OpenAI wire format. Adopting it instead of a bespoke protocol means:

- **Zero glue code** to integrate with Codex / Aider / Continue / Cursor / sgpt / any LangChain client.
- **Free streaming** via SSE — same shape OpenAI uses.
- **Multi-process** by default — your agent process is decoupled from the editor.
- **Function-calling** is auto-wired through `Microsoft.Extensions.AI`'s `UseFunctionInvocation` middleware, so any tool you register is callable by every supported model.

See the [project repository](https://github.com/rkttu/cadenza) for the full
specification, sample agents, and security notes.
