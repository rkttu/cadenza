# Cadenza.Templates

> Read this in [한국어](README.ko.md).

`dotnet new` project templates for the Cadenza single-file scripting SDK family. Each template produces a single `.cs` file ready to `dotnet run`.

## Install

```bash
dotnet new install Cadenza.Templates
```

## Use

| Short name | Variant | Produces |
| --- | --- | --- |
| `cadenza-console` (also `cadenza`) | `Cadenza` | Console script (shell, CLI, build glue) |
| `cadenza-worker` | `Cadenza.Worker` | Background service / daemon |
| `cadenza-web` | `Cadenza.Web` | Minimal API endpoint |
| `cadenza-mcp` | `Cadenza.Mcp` | MCP server for Claude Desktop / Cursor / VS Code AI |
| `cadenza-agent` | `Cadenza.Agent` | AI agent with OpenAI-compatible HTTP frontend (Ollama / OpenAI / Anthropic / Azure OpenAI) |

```bash
dotnet new cadenza -n mytool -o ./mytool   # alias for cadenza-console
cd mytool
dotnet run mytool.cs
```

Each starter pins the matching SDK version, lists the Tier 1 bare names available, and includes a comment with the publish command for a self-contained binary.

## Categorization

All five templates carry the `Cadenza` classification tag so they group together in Visual Studio's "New Project" dialog (once VS surfaces them as of a later release), while each also carries its own variant tag (`Console`, `Worker`, `Web` / `WebAPI`, `AI` / `MCP`, `AI` / `Agent`) so they appear under those filters too. The `defaultName` pre-fills a sensible project name per variant (e.g., `MyScript`, `MyWorker`, `MyApi`, `MyMcpServer`, `MyAgent`).

## Uninstall

```bash
dotnet new uninstall Cadenza.Templates
```

See the [project repository](https://github.com/rkttu/cadenza) for the full SDK family.
