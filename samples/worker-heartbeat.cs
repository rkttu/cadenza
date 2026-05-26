#!/usr/bin/env dotnet run
#:sdk Cadenza.Worker@1.0.13

// Minimal worker. Demonstrates:
//   - Worker.Run hosts a BackgroundService and respects Ctrl+C / SIGTERM
//   - the CancellationToken passed to the lambda fires on graceful shutdown
//   - Log.Info routes through ILogger so structured logging providers Just Work

await Run(async (ct) =>
{
    while (!ct.IsCancellationRequested)
    {
        Log.Info($"Heartbeat at {DateTime.UtcNow:O}");
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
    }
});
