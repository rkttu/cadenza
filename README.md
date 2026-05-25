# Cadenza

[![CI](https://github.com/rkttu/cadenza/actions/workflows/ci.yml/badge.svg)](https://github.com/rkttu/cadenza/actions/workflows/ci.yml)
[![Release](https://github.com/rkttu/cadenza/actions/workflows/release.yml/badge.svg)](https://github.com/rkttu/cadenza/actions/workflows/release.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0%2B-512BD4.svg?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

A single-file scripting SDK family for .NET 10+ file-based apps, distributed as three MSBuild SDK packages:

| SDK | Package | Use case |
| --- | --- | --- |
| `Cadenza` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.svg?label=nuget)](https://www.nuget.org/packages/Cadenza) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.svg?label=downloads)](https://www.nuget.org/packages/Cadenza) | Console scripts, CLI utilities |
| `Cadenza.Worker` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.Worker.svg?label=nuget)](https://www.nuget.org/packages/Cadenza.Worker) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.Worker.svg?label=downloads)](https://www.nuget.org/packages/Cadenza.Worker) | Background services, daemons |
| `Cadenza.Web` | [![NuGet](https://img.shields.io/nuget/vpre/Cadenza.Web.svg?label=nuget)](https://www.nuget.org/packages/Cadenza.Web) [![Downloads](https://img.shields.io/nuget/dt/Cadenza.Web.svg?label=downloads)](https://www.nuget.org/packages/Cadenza.Web) | Web APIs, Minimal API scripts |

Select a variant by adding a `#:sdk` directive to the first line of your script. **The version must be exact** — MSBuild SDK references do not support wildcards like `1.*`. Replace the version below with the latest from nuget.org:

```csharp
#:sdk Cadenza@1.0.1           // console
#:sdk Cadenza.Worker@1.0.1    // worker
#:sdk Cadenza.Web@1.0.1       // web
```

See [docs/spec.md](docs/spec.md) for the full v0.1 specification and [docs/publishing-single-binary.md](docs/publishing-single-binary.md) for distribution.

## Examples

Console:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.1

foreach (var file in Glob("**/*.md"))
    WriteLine($"{file}: {ReadText(file).Length:N0} bytes");
```

Worker:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Worker@1.0.1

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
#:sdk Cadenza.Web@1.0.1

Get("/", () => "Hello from Cadenza.Web");
Get("/health", () => new { status = "ok", time = DateTime.UtcNow });

await Run();
```

More samples under [samples/](samples/).

## Repository layout

```text
src/
  core/          # shared modules (namespace: Cadenza)
  worker/        # worker-only modules (namespace: Cadenza.Worker)
  web/           # web-only modules (namespace: Cadenza.Web)
packaging/
  Cadenza/             # console SDK package layout
  Cadenza.Worker/      # worker SDK package layout
  Cadenza.Web/         # web SDK package layout
build/
  Cadenza.Packaging.proj   # traversal project that packs all three SDKs
samples/                   # canonical example scripts
.github/workflows/         # CI (pack) and release (pack + push to nuget.org)
```

## Building locally

```bash
dotnet pack build/Cadenza.Packaging.proj -c Release -o ./artifacts -p:Version=0.1.0-local
```

Three `.nupkg` files appear under `./artifacts`. To consume them from a script, add a `nuget.config` next to the script with a `<add key="local" value="…/artifacts" />` source.

## Publishing

CI/CD is configured in [.github/workflows/](.github/workflows/):

- [ci.yml](.github/workflows/ci.yml) — runs on every push/PR; packs all three SDKs on Linux/macOS/Windows and uploads the Linux artifacts.
- [release.yml](.github/workflows/release.yml) — runs on `v*` tag push or `workflow_dispatch`; packs and pushes to nuget.org using the `NUGET_API_KEY` repository secret, then creates a GitHub release.

To cut a release: push a tag like `v1.0.0`, or trigger `release.yml` manually with a version input.

## Troubleshooting

Common issues and workarounds are collected in [docs/troubleshooting.md](docs/troubleshooting.md). Quick links:

- [`#:sdk Cadenza@1.*` (wildcard) gives "no version specified"](docs/troubleshooting.md#sdk-cadenza1-wildcard--floating-버전-사용-시-버전이-지정되지-않음-오류) — MSBuild SDK refs require exact versions, unlike `PackageReference`
- [Newly-released SDK version not picked up (stale NuGet cache)](docs/troubleshooting.md#새로-게시된-버전이-인식되지-않음-stale-nuget-cache) — clear only Cadenza-related cache entries
- [`MSB3552: **/*.resx not found` on macOS](docs/troubleshooting.md#macos에서-error-msb3552-리소스-파일-resx을를-찾을-수-없습니다) — fixed in 1.0.1

## License

[MIT](LICENSE) — © Cadenza contributors.
