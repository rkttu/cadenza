# Cadenza.Cli

> Read this in [한국어](README.ko.md).

`cadenza` is an **optional accessory CLI** for the [Cadenza single-file .NET scripting SDK family](https://github.com/rkttu/cadenza). The canonical way to run a Cadenza script remains `dotnet run app.cs` with an exact `#:sdk` version pinned in the script — installing this tool is not required.

What you get when you install it:

- A bundled **MSBuild SDK resolver** that gets activated on the **very first `cadenza` invocation** (any subcommand). After that, `dotnet run app.cs` works with `#:sdk Cadenza` (no version) or `#:sdk Cadenza@latest` directly.
- `cadenza new <variant>` — scaffolding shortcuts wrapping `dotnet new cadenza-*`.
- `cadenza run app.cs` / `cadenza publish app.cs` — wrappers that resolve `@latest` then forward to `dotnet run` / `dotnet publish`. Works even before the resolver is activated.

## Install

```bash
dotnet tool install -g Cadenza.Cli
cadenza --version          # also runs first-time setup of the SDK resolver
```

On Windows the user-level `MSBUILDADDITIONALSDKRESOLVERSFOLDER` environment variable is set automatically — open a fresh terminal afterwards. On macOS / Linux the CLI prints a one-line `export ...` snippet for your shell profile (`~/.bashrc`, `~/.zshrc`, etc.).

Set `CADENZA_SKIP_AUTOINSTALL=1` to opt out of the first-run setup (useful in CI / sandboxed environments).

## Use

```bash
# scaffold
cadenza new console -n mytool -o ./mytool   # alias: cli, c
cadenza new worker  -n mydaemon             # alias: w, svc, daemon
cadenza new web     -n myapi                # alias: api
cadenza new mcp     -n myserver             # alias: m, server

# run a script that pins @latest or omits the version
cat > app.cs <<'EOF'
#:sdk Cadenza@latest
WriteLine("hi");
EOF
cadenza run app.cs       # always works (the CLI rewrites @latest first)
dotnet run app.cs        # also works once the SDK resolver is active

# publish
cadenza publish app.cs -r linux-x64 -c Release
```

`@latest` (or an empty version) queries `https://api.nuget.org/v3-flatcontainer/cadenza/index.json` for the highest stable SemVer. The CLI caches the resolution at `~/.cadenza/cache/<pkgid>.version` for 24 hours; the SDK resolver doesn't cache (relies on the global NuGet packages folder).

## Two activation paths

| Path | What works |
| --- | --- |
| `dotnet run app.cs` (no CLI installed) | `#:sdk Cadenza@<exact-version>` — the canonical workflow, no install needed. |
| `cadenza run app.cs` (CLI installed, resolver bootstrap optional) | Adds `#:sdk Cadenza@latest` and version-less support via the wrapper. |
| `dotnet run app.cs` (CLI installed, resolver active in shell env) | Adds `#:sdk Cadenza@latest` and version-less support via the MSBuild resolver — works in IDEs / `dotnet publish` / any MSBuild entry point. |

## Explicit setup / teardown commands

```bash
cadenza install-resolver     # re-run the first-time setup explicitly (with full output)
cadenza uninstall-resolver   # remove the resolver and the env var (Windows)
```

`uninstall-resolver` does NOT uninstall the tool itself — use `dotnet tool uninstall -g Cadenza.Cli` for that.

## Default workflow is still `dotnet run`

The Cadenza family's primary entry point is and remains:

```bash
dotnet run app.cs
```

where `app.cs` pins an exact SDK version. That works without this CLI installed, and is the path documented across the project's README, samples, AI agent skills, and IDE templates.

This CLI exists for two narrow scenarios:

1. You want `@latest` semantics in script files (quick experiments where you don't care about the exact version).
2. You prefer `cadenza new <variant>` over `dotnet new cadenza-<variant>` for terseness.

If neither applies, you don't need to install this package.

## Uninstall

```bash
cadenza uninstall-resolver         # clean up resolver files + env var first
dotnet tool uninstall -g Cadenza.Cli
```

See the [project repository](https://github.com/rkttu/cadenza) for the full SDK family.
