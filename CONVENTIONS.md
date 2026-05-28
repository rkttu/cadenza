# CONVENTIONS.md

Aider reads this file by convention. The same Cadenza skill is also available
at `AGENTS.md`, `.github/copilot-instructions.md`, `.cursor/rules/cadenza.mdc`,
`.claude/skills/cadenza/SKILL.md`, and `.continue/rules/cadenza.md`. The
canonical source lives at [`skills/cadenza/SKILL.md`](skills/cadenza/SKILL.md).

---

# Cadenza skill

Cadenza is .NET's answer to "Python for scripting" with "Go-like single-binary distribution". Every Cadenza program is one `.cs` file that runs with `dotnet run app.cs` and publishes to a ~30–40 MB self-contained binary with `dotnet publish app.cs -r <rid> -c Release`.

Repo: <https://github.com/rkttu/cadenza>

## When to suggest Cadenza

| Goal | SDK | Use case |
| --- | --- | --- |
| CLI / shell automation / build glue | `Cadenza` | git deploy guards, file processing, devops scripts |
| Background service / daemon | `Cadenza.Worker` | heartbeat, polling, scheduled work |
| Minimal HTTP API / webhook | `Cadenza.Web` | a 30-line REST endpoint, internal tool |
| MCP server (AI tool integration) | `Cadenza.Mcp` | tools / resources / prompts for Claude Desktop, Cursor, VS Code AI |
| Local AI agent | `Cadenza.Agent` | OpenAI-compatible HTTP server fronting Ollama / OpenAI / Anthropic / Azure OpenAI — Codex / Aider / Continue / Cursor speak to it as if it were OpenAI |

Skip Cadenza for multi-project solutions, libraries that ship as DLLs, or anything that needs a full csproj.

## Critical: exact version pinning

**MSBuild SDK references do NOT support wildcards (`1.*`).** Always pin an exact SemVer version. Latest: `1.0.15`.

```csharp
#:sdk Cadenza@1.0.15           // console
#:sdk Cadenza.Worker@1.0.15    // worker
#:sdk Cadenza.Web@1.0.15       // web
#:sdk Cadenza.Mcp@1.0.15       // MCP server
#:sdk Cadenza.Agent@1.0.15     // AI agent (OpenAI-compatible HTTP)
```

## Tier 1 — bare names per variant

- **`Cadenza` (console)**: `Run(cmd)`, `Capture(cmd)`, `ReadText(path)`, `WriteText(path, content)`, `Glob(pattern)`, `TempDir()`, plus `WriteLine` / `Write` / `ReadLine`.
- **`Cadenza.Worker`**: `Run(Func<CT, Task>)`, `Config<T>(key)`. `Log.Info/Warn/Error/Debug`.
- **`Cadenza.Web`**: `Get/Post/Put/Delete/Map(path, handler)`, `Run()`. `Web.App` / `Web.Services` for raw access.
- **`Cadenza.Mcp`**: `Tool(name, desc, handler)`, `Resource(uri, name, handler)`, `Prompt(name, desc, handler)`, `Run()`. `Log.*` → stderr (never use `WriteLine` here).
- **`Cadenza.Agent`**: `Tool(name, desc, handler)`, `SystemPrompt(text)`, `UseOllama/UseOpenAi/UseAnthropic/UseAzureOpenAi/UseChatClient`, `Run()` (HTTP server on `localhost:8080` — serves `POST /v1/chat/completions` AND `POST /v1/responses`), `ChatLoop()` (REPL), `Reply(prompt)` (one-shot). Server-side `Tool(...)` auto-invokes on Chat Completion only; Codex (Responses path) brings its own toolset.

## Gotchas

1. **MCP scripts NEVER write to stdout.** stdio carries JSON-RPC; stray text disconnects the client. Use `Log.*` (stderr).
2. **Wildcards in `#:sdk` don't work.** Pin exact.
3. **NativeAOT is opt-in.** Add `#:property PublishAot=true` at top of script.
4. **JSON requires `JsonSerializerContext`** (source-generated) — no reflection overloads.
5. **`Prompt.*` in CI:** set `CADENZA_PROMPT_<NAME>` env var.
6. **Working directory is the directory holding the `.cs` file.** Use `Env.ScriptPath` / `Env.ScriptDirectory` to read the script's own path inside the program (both null after `dotnet publish`).
7. **Transitive `#:include` works out of the box.** A `#:package` / `#:property` inside an `#:include`d helper file is honored — every Cadenza SDK enables `ExperimentalFileBasedProgramEnableTransitiveDirectives` in its `Sdk.props`. To be removed once the .NET SDK makes it always-on.

## Canonical patterns

### Console: git-aware deploy gate

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.15

var branch = Capture("git rev-parse --abbrev-ref HEAD").Trim();
if (branch != "main") { WriteLine($"Refusing to deploy from '{branch}'"); Env.Exit(1); }

if (Run("dotnet test", throwOnError: false) != 0) Env.Exit(2);
Run("dotnet publish -c Release -o ./dist", throwOnError: true);
```

### Console: AOT-clean HTTP fetch with typed JSON

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.15

using System.Text.Json.Serialization;

var repo = await Http.GetJson<Repo>("https://api.github.com/repos/dotnet/runtime", Ctx.Default);
WriteLine($"{repo.full_name}: {repo.stargazers_count:N0} stars");

record Repo(string full_name, int stargazers_count);
[JsonSerializable(typeof(Repo))]
partial class Ctx : JsonSerializerContext { }
```

### Worker

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Worker@1.0.15

await Run(async (ct) =>
{
    while (!ct.IsCancellationRequested)
    {
        Log.Info($"tick {DateTime.UtcNow:O}");
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
    }
});
```

### Web

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Web@1.0.15

Get("/", () => "hello");
Get("/health", () => new { status = "ok", time = DateTime.UtcNow });
Post("/echo", (EchoRequest req) => new EchoResponse(req.Message.ToUpper()));

await Run();

record EchoRequest(string Message);
record EchoResponse(string Echoed);
```

### MCP server

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Mcp@1.0.15

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern",
    (string pattern) => Glob(pattern).ToArray());

await Run();
```

### AI agent (OpenAI-compatible HTTP)

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.15

SystemPrompt("You are a coding assistant. Ground answers in real files.");

Tool("read_file", "Read a UTF-8 text file from the working directory",
    (string path) => ReadText(path));

UseOllama("qwen2.5-coder:7b");

await Run();   // POST http://localhost:8080/v1/chat/completions
```

Point Codex / Aider / Continue / Cursor at it via `OPENAI_BASE_URL=http://localhost:8080/v1`.

## Tier 2 — prefixed modules (shared)

- `Sh.Run/Capture/Pipe/RunAsync/CaptureAsync`
- `Fs.ReadText/WriteText/ReadBytes/WriteBytes/Exists/Delete/Move/Copy/MakeDir/Glob/TempDir/ReadTextAsync`
- `Http.GetJson<T>/PostJson<TReq,TResp>/GetText/Download` + `Http.Client`
- `Env.Get/Args/Cwd/Exit/IsCi/IsWindows/IsMacOS/IsLinux/ScriptPath/ScriptDirectory`
- `Prompt.Confirm/Select/Text/Password` (console + worker only)
- `Json.Parse<T>/Stringify<T>` (always takes a `JsonSerializerContext`)

## Reference

- README: <https://github.com/rkttu/cadenza/blob/main/README.md>
- Spec: <https://github.com/rkttu/cadenza/blob/main/docs/spec.md>
- Samples: <https://github.com/rkttu/cadenza/tree/main/samples>
