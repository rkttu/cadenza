# Cadenza.Worker

> Read this in [한국어](README.ko.md).

`Cadenza.Worker` is the worker / daemon variant of the Cadenza SDK family — a single-file scripting MSBuild SDK that wraps `Microsoft.NET.Sdk.Worker`.

## Quick start

Create a `heartbeat.cs` file:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Worker@1.0.12

await Run(async (ct) =>
{
    while (!ct.IsCancellationRequested)
    {
        Log.Info($"Heartbeat at {DateTime.UtcNow:O}");
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
    }
});
```

Run iteratively:

```bash
dotnet run heartbeat.cs
```

Publish as a self-contained single binary:

```bash
dotnet publish heartbeat.cs -r linux-x64 -c Release
```

See the [project repository](https://github.com/rkttu/cadenza) for the full specification.
