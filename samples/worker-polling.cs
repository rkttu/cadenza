#!/usr/bin/env dotnet run
#:sdk Cadenza.Worker@1.0.15

using System.Text.Json.Serialization;

// Poll a remote HTTP endpoint and log status changes. Demonstrates:
//   - Worker.Config<T> for typed configuration (read from env vars / appsettings)
//   - Cadenza's HTTP client inside a worker
//   - Log.* for level-correct logging
//   - graceful shutdown on the cancellation token

var endpoint = Worker.Config<string>("Api:Endpoint");
var intervalSec = Worker.Config<int>("Api:IntervalSeconds");

string? lastStatus = null;

await Run(async (ct) =>
{
    Log.Info($"Polling {endpoint} every {intervalSec}s");

    while (!ct.IsCancellationRequested)
    {
        try
        {
            var probe = await Http.GetJson<HealthProbe>($"{endpoint}/health", PollCtx.Default, ct);
            if (probe.status != lastStatus)
            {
                Log.Info($"Status changed: {lastStatus ?? "(initial)"} -> {probe.status}");
                lastStatus = probe.status;
            }
            else
            {
                Log.Debug($"Status unchanged: {probe.status}");
            }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            Log.Warn($"Probe failed: {ex.Message}");
        }

        await Task.Delay(TimeSpan.FromSeconds(intervalSec), ct);
    }
});

record HealthProbe(string status, DateTime time);

[JsonSerializable(typeof(HealthProbe))]
partial class PollCtx : JsonSerializerContext { }
