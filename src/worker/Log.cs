using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cadenza.Worker;

public static class Log
{
    private static ILogger? _cached;

    private static ILogger Logger =>
        _cached ??= Worker.Host.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Cadenza");

    public static void Info(string message) => Logger.LogInformation("{Message}", message);
    public static void Warn(string message) => Logger.LogWarning("{Message}", message);
    public static void Error(string message, Exception? ex = null) => Logger.LogError(ex, "{Message}", message);
    public static void Debug(string message) => Logger.LogDebug("{Message}", message);
}
