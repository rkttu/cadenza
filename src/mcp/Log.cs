using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cadenza.Mcp;

/// <summary>
/// Logging shortcuts routed through <see cref="ILogger"/> from the MCP host's
/// service provider. Output goes to stderr — critical in the stdio variant
/// where stdout carries the MCP JSON-RPC protocol and any stray text breaks
/// the client connection.
/// </summary>
/// <remarks>
/// Available from inside tool/resource/prompt handlers and after
/// <see cref="Mcp.Run"/> has been invoked. Calling it before the host is
/// built throws.
/// </remarks>
public static class Log
{
    private static ILogger? _cached;

    private static ILogger Logger =>
        _cached ??= Mcp.Host.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Cadenza.Mcp");

    /// <summary>Emit an <see cref="LogLevel.Information"/> message to stderr.</summary>
    public static void Info(string message) => Logger.LogInformation("{Message}", message);

    /// <summary>Emit a <see cref="LogLevel.Warning"/> message to stderr.</summary>
    public static void Warn(string message) => Logger.LogWarning("{Message}", message);

    /// <summary>Emit a <see cref="LogLevel.Error"/> message to stderr, optionally with the originating exception.</summary>
    public static void Error(string message, Exception? ex = null) => Logger.LogError(ex, "{Message}", message);

    /// <summary>Emit a <see cref="LogLevel.Debug"/> message to stderr (visible only when the configured minimum level permits it).</summary>
    public static void Debug(string message) => Logger.LogDebug("{Message}", message);
}
