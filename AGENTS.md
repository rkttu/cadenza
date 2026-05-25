# AGENTS.md

Instructions for AI coding agents working in this repository, and a portable Cadenza skill for any agent that recognizes this filename.

Repo: <https://github.com/rkttu/cadenza>

## About this repo

This is the **Cadenza SDK family** — a .NET 10+ single-file scripting SDK distributed as four MSBuild SDK packages on nuget.org:

- `Cadenza` (console scripts, CLI utilities)
- `Cadenza.Worker` (background services, daemons)
- `Cadenza.Web` (Minimal API endpoints)
- `Cadenza.Mcp` (Model Context Protocol servers for AI agent tools)

If you are helping a user **write a Cadenza script in their own project**, the rest of this document is the user-facing skill: tells you when to suggest Cadenza and how to write idiomatic Cadenza code.

If you are helping **maintain this repository**, see also `docs/spec.md` (Korean spec), `docs/troubleshooting.md`, and the per-package READMEs under `packaging/`.

---

# Cadenza skill

Cadenza is .NET's answer to "Python for scripting" with "Go-like single-binary distribution". Every Cadenza program is one `.cs` file that runs with `dotnet run app.cs` and publishes to a ~30–40 MB self-contained binary with `dotnet publish app.cs -r <rid> -c Release`.

## When to suggest Cadenza

| Goal | SDK | Use case |
| --- | --- | --- |
| CLI / shell automation / build glue | `Cadenza` | git deploy guards, file processing, devops scripts |
| Background service / daemon | `Cadenza.Worker` | heartbeat, polling, scheduled work |
| Minimal HTTP API / webhook | `Cadenza.Web` | a 30-line REST endpoint, internal tool |
| MCP server (AI tool integration) | `Cadenza.Mcp` | tools / resources / prompts for Claude Desktop, Cursor, VS Code AI |

Skip Cadenza for multi-project solutions, libraries that ship as DLLs, or anything that needs a full csproj. Cadenza is for the "single file = whole program" case.

## Critical: exact version pinning

**MSBuild SDK references do NOT support wildcards (`1.*`).** Always pin an exact SemVer version. Latest: `1.0.7`.

```csharp
#:sdk Cadenza@1.0.7           // console
#:sdk Cadenza.Worker@1.0.7    // worker
#:sdk Cadenza.Web@1.0.7       // web
#:sdk Cadenza.Mcp@1.0.7       // MCP server
```

## Tier 1 — bare names per variant (no namespace prefix needed)

- **`Cadenza` (console)**: `Run(cmd)`, `Capture(cmd)`, `ReadText(path)`, `WriteText(path, content)`, `Glob(pattern)`, `TempDir()`, plus `WriteLine` / `Write` / `ReadLine`.
- **`Cadenza.Worker`**: `Run(Func<CT, Task>)`, `Config<T>(key)`. `Log.Info/Warn/Error/Debug` via ILogger.
- **`Cadenza.Web`**: `Get/Post/Put/Delete/Map(path, handler)`, `Run()`. `Web.App` / `Web.Services` for raw access.
- **`Cadenza.Mcp`**: `Tool(name, desc, handler)`, `Resource(uri, name, handler)`, `Prompt(name, desc, handler)`, `Run()`. `Log.*` → stderr (never use `WriteLine` here).

## Gotchas (these have bitten users)

1. **MCP scripts NEVER write to stdout.** stdio carries JSON-RPC; stray text disconnects the client. `Cadenza.Mcp` intentionally does not expose `WriteLine` as a bare name. Use `Log.*` (stderr).
2. **Wildcards in `#:sdk` don't work.** Pin exact. Use `global.json` `msbuild-sdks` to centralize.
3. **NativeAOT is opt-in.** Default is JIT + R2R, ~30–40 MB. Add `#:property PublishAot=true` at the top of the script for a ~10–30 MB AOT binary.
4. **JSON requires `JsonSerializerContext` (source-generated).** `Http.GetJson<T>(url, ctx)` and `Json.Parse<T>(json, ctx)` take an explicit context — no reflection overloads.
5. **`Prompt.*` in CI:** set `CADENZA_PROMPT_<NAME>` env var (NAME = uppercased question, non-alphanumerics → `_`).
6. **Working directory is the directory holding the `.cs` file.** Globs and relative paths resolve from there.

## Canonical patterns

### Console: git-aware deploy gate

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.7

var branch = Capture("git rev-parse --abbrev-ref HEAD").Trim();
if (branch != "main") { WriteLine($"Refusing to deploy from '{branch}'"); Env.Exit(1); }

if (Run("dotnet test", throwOnError: false) != 0) Env.Exit(2);
Run("dotnet publish -c Release -o ./dist", throwOnError: true);
```

### Console: AOT-clean HTTP fetch with typed JSON

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.7

using System.Text.Json.Serialization;

Http.Client.DefaultRequestHeaders.UserAgent.ParseAdd("my-script/1.0");
var repo = await Http.GetJson<Repo>("https://api.github.com/repos/dotnet/runtime", Ctx.Default);
WriteLine($"{repo.full_name}: {repo.stargazers_count:N0} stars");

record Repo(string full_name, int stargazers_count);

[JsonSerializable(typeof(Repo))]
partial class Ctx : JsonSerializerContext { }
```

### Worker: periodic loop with graceful shutdown

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Worker@1.0.7

await Run(async (ct) =>
{
    while (!ct.IsCancellationRequested)
    {
        Log.Info($"tick {DateTime.UtcNow:O}");
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
    }
});
```

### Web: Minimal API with record binding

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Web@1.0.7

Get("/", () => "hello");
Get("/health", () => new { status = "ok", time = DateTime.UtcNow });
Post("/echo", (EchoRequest req) => new EchoResponse(req.Message.ToUpper()));

await Run();

record EchoRequest(string Message);
record EchoResponse(string Echoed);
```

### MCP server for Claude Desktop / Cursor / VS Code AI

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Mcp@1.0.7

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern (e.g., **/*.cs)",
    (string pattern) => Glob(pattern).ToArray());

await Run();
```

Client config:

```json
{
  "mcpServers": {
    "cadenza-files": {
      "command": "dotnet",
      "args": ["run", "/absolute/path/to/server.cs"]
    }
  }
}
```

## Deployment

```bash
# Default: SCD + R2R + SingleFile + Compression (~30-40 MB)
dotnet publish app.cs -r linux-x64 -c Release

# NativeAOT opt-in: add `#:property PublishAot=true` at the top of the script
```

Supported RIDs: `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`, `win-x64`, `win-arm64`.

## Tier 2 — prefixed modules (shared by all variants)

- `Sh.Run/Capture/Pipe/RunAsync/CaptureAsync`
- `Fs.ReadText/WriteText/ReadBytes/WriteBytes/Exists/Delete/Move/Copy/MakeDir/Glob/TempDir/ReadTextAsync`
- `Http.GetJson<T>/PostJson<TReq,TResp>/GetText/Download` + `Http.Client`
- `Env.Get/Args/Cwd/Exit/IsCi/IsWindows/IsMacOS/IsLinux`
- `Prompt.Confirm/Select/Text/Password` (console + worker only)
- `Json.Parse<T>/Stringify<T>` (always takes a `JsonSerializerContext`)

## Reference

- README: <https://github.com/rkttu/cadenza/blob/main/README.md>
- Spec (Korean): <https://github.com/rkttu/cadenza/blob/main/docs/spec.md>
- Publishing guide: <https://github.com/rkttu/cadenza/blob/main/docs/publishing-single-binary.md>
- Troubleshooting: <https://github.com/rkttu/cadenza/blob/main/docs/troubleshooting.md>
- Samples (9 progressive examples): <https://github.com/rkttu/cadenza/tree/main/samples>
