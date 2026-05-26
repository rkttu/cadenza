#!/usr/bin/env dotnet run
#:sdk Cadenza.Worker@1.0.14

// Cadenza worker script. Tier 1 bare names:
//   Run(Func<CT, Task> work)     — start host + BackgroundService
//   Config<T>(key)               — IConfiguration shortcut
//   Log.Info/Warn/Error/Debug    — ILogger routing
//   ReadText/WriteText/Glob/WriteLine (shared with Cadenza)
//
// Run with:    dotnet run app.cs
// Publish to a self-contained binary:
//   dotnet publish app.cs -r linux-x64 -c Release
//
// See: https://github.com/rkttu/cadenza

await Run(async (ct) =>
{
    while (!ct.IsCancellationRequested)
    {
        Log.Info($"tick {DateTime.UtcNow:O}");
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
    }
});
