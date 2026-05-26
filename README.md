# Cadenza

> Read this in [한국어](README.ko.md).

[![CI](https://github.com/rkttu/cadenza/actions/workflows/ci.yml/badge.svg)](https://github.com/rkttu/cadenza/actions/workflows/ci.yml)
[![Release](https://github.com/rkttu/cadenza/actions/workflows/release.yml/badge.svg)](https://github.com/rkttu/cadenza/actions/workflows/release.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0%2B-512BD4.svg?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

A single-file scripting SDK family for .NET 10+ file-based apps, distributed as five MSBuild SDK packages:

| SDK | Package | Use case |
| --- | --- | --- |
| `Cadenza` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.svg?label=nuget)](https://www.nuget.org/packages/Cadenza) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.svg?label=downloads)](https://www.nuget.org/packages/Cadenza) | Console scripts, CLI utilities |
| `Cadenza.Worker` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.Worker.svg?label=nuget)](https://www.nuget.org/packages/Cadenza.Worker) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.Worker.svg?label=downloads)](https://www.nuget.org/packages/Cadenza.Worker) | Background services, daemons |
| `Cadenza.Web` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.Web.svg?label=nuget)](https://www.nuget.org/packages/Cadenza.Web) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.Web.svg?label=downloads)](https://www.nuget.org/packages/Cadenza.Web) | Web APIs, Minimal API scripts |
| `Cadenza.Mcp` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.Mcp.svg?label=nuget)](https://www.nuget.org/packages/Cadenza.Mcp) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.Mcp.svg?label=downloads)](https://www.nuget.org/packages/Cadenza.Mcp) | MCP servers for Claude / Cursor / VS Code AI agents |
| `Cadenza.Agent` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.Agent.svg?label=nuget)](https://www.nuget.org/packages/Cadenza.Agent) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.Agent.svg?label=downloads)](https://www.nuget.org/packages/Cadenza.Agent) | Local AI agents — serves **both** OpenAI Chat Completion (Aider / Continue / Cursor / Copilot BYOK) and OpenAI Responses API (Codex CLI) over the same `IChatClient` backend (Ollama / OpenAI / Anthropic / Azure OpenAI) |

Plus a companion `dotnet new` template package [`Cadenza.Templates`](https://www.nuget.org/packages/Cadenza.Templates) that ships starters for all five variants (see [Bootstrap a new script](#bootstrap-a-new-script-with-dotnet-new) below).

Select a variant by adding a `#:sdk` directive to the first line of your script. **The version must be exact** — MSBuild SDK references do not support wildcards like `1.*`. Replace the version below with the latest from nuget.org:

```csharp
#:sdk Cadenza@1.0.13           // console
#:sdk Cadenza.Worker@1.0.13    // worker
#:sdk Cadenza.Web@1.0.13       // web
#:sdk Cadenza.Mcp@1.0.13       // MCP server
#:sdk Cadenza.Agent@1.0.13     // AI agent (OpenAI-compatible HTTP server)
```

See [docs/spec.md](docs/spec.md) for the full specification (Korean) and [docs/publishing-single-binary.md](docs/publishing-single-binary.md) for distribution.

## Examples

Console:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.13

foreach (var file in Glob("**/*.md"))
    WriteLine($"{file}: {ReadText(file).Length:N0} bytes");
```

Worker:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Worker@1.0.13

await Run(async (ct) =>
{
    while (!ct.IsCancellationRequested)
    {
        Log.Info($"Heartbeat at {DateTime.UtcNow:O}");
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
    }
});
```

Web:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Web@1.0.13

Get("/", () => "Hello from Cadenza.Web");
Get("/health", () => new { status = "ok", time = DateTime.UtcNow });

await Run();
```

MCP server:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Mcp@1.0.13

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern",
    (string pattern) => Glob(pattern).ToArray());

await Run();
```

AI agent (OpenAI-compatible HTTP server — Codex / Aider / Continue / Cursor talk to it as if it were OpenAI):

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.13

SystemPrompt("You are a helpful assistant with filesystem access.");

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

UseOllama("llama3.2");      // or UseOpenAi / UseAnthropic / UseAzureOpenAi

await Run();                // serves OpenAI Chat Completion on http://localhost:8080
```

More samples under [samples/](samples/) — see the [sample index](samples/README.md) for the full list (console glob/grep, git deploy guard, JSON-typed HTTP fetch, interactive setup, worker with config polling, web CRUD API, MCP servers, AI agents).

### Bootstrap a new script with `dotnet new`

A single template package, [`Cadenza.Templates`](https://www.nuget.org/packages/Cadenza.Templates), provides starters for all five variants:

```bash
dotnet new install Cadenza.Templates
dotnet new cadenza         -n mytool   -o ./mytool      # console (alias of cadenza-console)
dotnet new cadenza-worker  -n mydaemon -o ./mydaemon    # background service
dotnet new cadenza-web     -n myapi    -o ./myapi       # Minimal API
dotnet new cadenza-mcp     -n myserver -o ./myserver    # MCP server
dotnet new cadenza-agent   -n myagent  -o ./myagent     # AI agent (OpenAI-compatible HTTP)
```

The bare `cadenza` short name is an alias for the console variant — `dotnet new cadenza-console` works too. Each command produces a single `.cs` file (named after `-n`) pre-pinned to the matching SDK version with a canonical starter pattern in the body.

## Repository layout

```text
src/
  core/          # shared modules (namespace: Cadenza)
  worker/        # worker-only modules (namespace: Cadenza.Worker)
  web/           # web-only modules (namespace: Cadenza.Web)
  mcp/           # MCP-only modules (namespace: Cadenza.Mcp)
  agent/         # agent-only modules (namespace: Cadenza.Agent)
packaging/
  Cadenza/             # console SDK package layout
  Cadenza.Worker/      # worker SDK package layout
  Cadenza.Web/         # web SDK package layout
  Cadenza.Mcp/         # MCP server SDK package layout
  Cadenza.Agent/       # AI agent SDK package layout
build/
  Cadenza.Packaging.proj   # traversal project that packs all five SDKs
samples/                   # canonical example scripts
.github/workflows/         # CI (pack) and release (pack + push to nuget.org)
```

## Building locally

```bash
dotnet pack build/Cadenza.Packaging.proj -c Release -o ./artifacts -p:Version=1.0.13-local
```

Four `.nupkg` files appear under `./artifacts`. To consume them from a script, add a `nuget.config` next to the script with a `<add key="local" value="…/artifacts" />` source.

## Publishing

CI/CD is configured in [.github/workflows/](.github/workflows/):

- [ci.yml](.github/workflows/ci.yml) — runs on every push/PR; packs all SDKs on Linux/macOS/Windows and uploads the Linux artifacts.
- [release.yml](.github/workflows/release.yml) — runs on `v*` tag push or `workflow_dispatch`; packs and pushes to nuget.org using the `NUGET_API_KEY` repository secret, then creates a GitHub release.

To cut a release: push a tag like `v1.0.7`, or trigger `release.yml` manually with a version input.

## AI coding agent integration

Cadenza ships with adapter files for the major AI coding agents so they
discover Cadenza automatically and write idiomatic scripts. The canonical
skill content lives at [`skills/cadenza/SKILL.md`](skills/cadenza/SKILL.md);
adapters are pre-installed at the well-known path for each agent:

| Agent | Adapter path |
| --- | --- |
| Universal (Cody, OpenAI tools, …) | [`AGENTS.md`](AGENTS.md) |
| Aider | [`CONVENTIONS.md`](CONVENTIONS.md) |
| GitHub Copilot | [`.github/copilot-instructions.md`](.github/copilot-instructions.md) |
| Cursor | [`.cursor/rules/cadenza.mdc`](.cursor/rules/cadenza.mdc) |
| Claude Code | [`.claude/skills/cadenza/SKILL.md`](.claude/skills/cadenza/SKILL.md) |
| Continue | [`.continue/rules/cadenza.md`](.continue/rules/cadenza.md) |

To enable Cadenza-aware assistance in any other project, copy the adapter for
your agent — see [`skills/README.md`](skills/README.md) for one-liner curl
recipes.

## Troubleshooting

Common issues and workarounds are collected in [docs/troubleshooting.md](docs/troubleshooting.md). Quick links:

- [`#:sdk Cadenza@1.*` (wildcard) gives "no version specified"](docs/troubleshooting.md#sdk-cadenza1-wildcard--floating-version-errors-with-no-version-specified) — MSBuild SDK refs require exact versions, unlike `PackageReference`
- [Newly-released SDK version not picked up (stale NuGet cache)](docs/troubleshooting.md#newly-released-version-not-picked-up-stale-nuget-cache) — clear only Cadenza-related cache entries
- [`MSB3552: **/*.resx not found` on macOS](docs/troubleshooting.md#macos-error-msb3552-resource-file-resx-could-not-be-found) — fixed in 1.0.1
- [CJK / emoji garbled in `Capture(...)` output on Windows](docs/troubleshooting.md#capture-output-cjk--emoji-garbled-on-windows) — fixed in 1.0.4

## License

[MIT](LICENSE) — © Cadenza contributors.
