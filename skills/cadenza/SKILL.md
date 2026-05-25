---
name: cadenza
description: Cadenza is a .NET 10+ single-file scripting SDK family. Use it when a user wants a short C# script for shell automation, a background service, a Minimal API endpoint, or an MCP server — each as a single `.cs` file that runs with `dotnet run` and publishes to a self-contained binary.
---

# Cadenza skill

Cadenza is .NET's answer to "Python for scripting" with "Go-like single-binary distribution". Every Cadenza program is one `.cs` file that runs with `dotnet run app.cs` and publishes to a ~30–40 MB self-contained binary with `dotnet publish app.cs -r <rid> -c Release`.

Repo: <https://github.com/rkttu/cadenza>

## When to suggest Cadenza

Pick Cadenza when the user wants:

| Goal | SDK | Use case |
| --- | --- | --- |
| CLI / shell automation / build glue | `Cadenza` | git deploy guards, file processing, devops scripts |
| Background service / daemon | `Cadenza.Worker` | heartbeat, polling, scheduled work |
| Minimal HTTP API / webhook | `Cadenza.Web` | a 30-line REST endpoint, internal tool |
| MCP server (AI tool integration) | `Cadenza.Mcp` | tools / resources / prompts for Claude Desktop, Cursor, VS Code AI |

Skip Cadenza for multi-project solutions, libraries that ship as DLLs, or anything that needs a full csproj. Cadenza is for the "single file = whole program" case.

## Critical: exact version pinning

**MSBuild SDK references do NOT support wildcards (`1.*`).** Always pin an exact SemVer version. Check nuget.org for the latest, then write the exact version on the `#:sdk` line:

```csharp
#:sdk Cadenza@1.0.7           // console
#:sdk Cadenza.Worker@1.0.7    // worker
#:sdk Cadenza.Web@1.0.7       // web
#:sdk Cadenza.Mcp@1.0.7       // MCP server
```

Latest versions (as of this skill update): all four SDKs at `1.0.7`.

## Tier 1 — bare names per variant (no namespace prefix needed)

### `Cadenza` (console)

| Name | Signature | Purpose |
| --- | --- | --- |
| `Run` | `int Run(string cmd, bool throwOnError = false)` | Run a shell command, return exit code |
| `Capture` | `string Capture(string cmd)` | Run a shell command, return stdout (throws on non-zero) |
| `ReadText` | `string ReadText(string path)` | Read a UTF-8 text file |
| `WriteText` | `void WriteText(string path, string content)` | Write a UTF-8 text file |
| `Glob` | `IEnumerable<string> Glob(string pattern)` | Match files; supports `**` and `*` |
| `TempDir` | `TempDirectory TempDir()` | `using var t = TempDir();` auto-cleanup |
| `WriteLine` / `Write` / `ReadLine` | (standard `System.Console`) | stdout / stdin |

### `Cadenza.Worker`

| Name | Signature | Purpose |
| --- | --- | --- |
| `Run` | `Task Run(Func<CancellationToken, Task> work)` | Start host + BackgroundService |
| `Config<T>` | `T Config<T>(string key)` | `IConfiguration` shortcut (env vars, appsettings, etc.) |
| Shared from console | `ReadText`, `WriteText`, `Glob`, `WriteLine` | same as `Cadenza` |

Plus `Log.Info(msg)`, `Log.Warn(msg)`, `Log.Error(msg, ex?)`, `Log.Debug(msg)` — routed through `ILogger`.

### `Cadenza.Web`

| Name | Signature | Purpose |
| --- | --- | --- |
| `Get` / `Post` / `Put` / `Delete` / `Map` | `(string path, Delegate handler)` | Minimal API route map |
| `Run` | `Task Run()` | Start the web server |
| Shared from console | `ReadText`, `WriteText`, `Glob` | same as `Cadenza` |

`Web.App` and `Web.Services` are the escape hatches when you need raw `WebApplication` / `IServiceCollection`.

### `Cadenza.Mcp`

| Name | Signature | Purpose |
| --- | --- | --- |
| `Tool` | `void Tool(string name, string description, Delegate handler)` | Register an MCP tool the AI client can invoke |
| `Resource` | `void Resource(string uri, string name, Delegate handler)` | Register an MCP resource |
| `Prompt` | `void Prompt(string name, string description, Delegate handler)` | Register an MCP prompt template |
| `Run` | `Task Run()` | Start the MCP server on stdio |
| Shared from console | `ReadText`, `WriteText`, `Glob` | same as `Cadenza` |

Plus `Log.Info/Warn/Error/Debug` routed to **stderr** (CRITICAL — see gotcha below).

## Gotchas (read every one — they have bitten users)

1. **MCP scripts must NEVER write to stdout.** The stdio transport carries JSON-RPC on stdout; any stray text disconnects the client. The SDK intentionally does NOT expose `WriteLine` / `Write` / `ReadLine` as bare names in `Cadenza.Mcp`. Use `Log.*` (stderr) for diagnostics.

2. **Wildcards in `#:sdk` don't work.** `Cadenza@1.*` errors with "no version specified". Pin exact. To centralize version bumps, use `global.json` `msbuild-sdks` instead.

3. **NativeAOT is opt-in.** Default deployment is JIT-with-R2R, ~30–40 MB binary. To get a ~10–30 MB AOT binary, add `#:property PublishAot=true` at the top of the script. All deps must be AOT-compatible (Cadenza's own surface already is).

4. **JSON requires `JsonSerializerContext` (source-generated).** `Http.GetJson<T>(url, ctx)` and `Json.Parse<T>(json, ctx)` both take an explicit `JsonSerializerContext` so scripts stay AOT-clean. No reflection-based overloads exist.

5. **`Prompt.*` in CI:** set `CADENZA_PROMPT_<NAME>` env var (NAME = the question text uppercased, non-alphanumerics → `_`). `Prompt.Password` throws in CI without such an env var (no safe default).

6. **The synthetic project's working directory is the directory holding the `.cs` file.** Glob patterns and relative paths resolve from there.

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

Register with the client:

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

# NativeAOT opt-in: add to the top of the script first
#   #:property PublishAot=true
# Then publish normally — produces a ~10-30 MB native binary
dotnet publish app.cs -r linux-x64 -c Release
```

Supported RIDs: `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`, `win-x64`, `win-arm64`.

## Tier 2 — prefixed modules (shared by all variants)

- `Sh.Run/Capture/Pipe/RunAsync/CaptureAsync` — shell exec with async variants
- `Fs.ReadText/WriteText/ReadBytes/WriteBytes/Exists/Delete/Move/Copy/MakeDir/Glob/TempDir/ReadTextAsync`
- `Http.GetJson<T>/PostJson<TReq,TResp>/GetText/Download` and `Http.Client` (shared `HttpClient` singleton)
- `Env.Get/Args/Cwd/Exit/IsCi/IsWindows/IsMacOS/IsLinux`
- `Prompt.Confirm/Select/Text/Password` (console + worker only)
- `Json.Parse<T>/Stringify<T>` (always takes a `JsonSerializerContext`)

## Reference

- Project README: <https://github.com/rkttu/cadenza/blob/main/README.md>
- Spec (Korean): <https://github.com/rkttu/cadenza/blob/main/docs/spec.md>
- Publishing guide: <https://github.com/rkttu/cadenza/blob/main/docs/publishing-single-binary.md>
- Troubleshooting: <https://github.com/rkttu/cadenza/blob/main/docs/troubleshooting.md>
- Samples (9 progressive examples): <https://github.com/rkttu/cadenza/tree/main/samples>
