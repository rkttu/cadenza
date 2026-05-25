using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cadenza.Worker;

/// <summary>
/// Logging shortcuts routed through <see cref="ILogger"/> from the worker
/// host's service provider. Must be called from within (or after)
/// <see cref="Worker.Run(System.Func{System.Threading.CancellationToken, System.Threading.Tasks.Task})"/>.
/// </summary>
public static class Log
{
    private static ILogger? _cached;

    private static ILogger Logger =>
        _cached ??= Worker.Host.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Cadenza");

    /// <summary>Emit an <see cref="LogLevel.Information"/> message.</summary>
    public static void Info(string message) => Logger.LogInformation("{Message}", message);

    /// <summary>Emit a <see cref="LogLevel.Warning"/> message.</summary>
    public static void Warn(string message) => Logger.LogWarning("{Message}", message);

    /// <summary>Emit a <see cref="LogLevel.Error"/> message, optionally with the originating exception.</summary>
    public static void Error(string message, Exception? ex = null) => Logger.LogError(ex, "{Message}", message);

    /// <summary>Emit a <see cref="LogLevel.Debug"/> message (visible only when the configured minimum level permits it).</summary>
    public static void Debug(string message) => Logger.LogDebug("{Message}", message);
}
