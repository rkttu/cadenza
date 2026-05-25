# Cadenza.Web

> Read this in [한국어](README.ko.md).

`Cadenza.Web` is the web / Minimal API variant of the Cadenza SDK family — a single-file scripting MSBuild SDK that wraps `Microsoft.NET.Sdk.Web`.

## Quick start

Create an `api.cs` file:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Web@1.0.7

Get("/", () => "Hello from Cadenza.Web");
Get("/health", () => new { status = "ok", time = DateTime.UtcNow });

await Run();
```

Run iteratively:

```bash
dotnet run api.cs
```

Publish as a self-contained single binary:

```bash
dotnet publish api.cs -r linux-x64 -c Release
```

See the [project repository](https://github.com/rkttu/cadenza) for the full specification.
