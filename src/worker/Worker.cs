using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cadenza.Worker;

/// <summary>
/// Worker host wrapper. Wraps <see cref="Microsoft.Extensions.Hosting"/> so a
/// single-file script can <c>await Run(async ct => { ... })</c> and get a
/// fully configured generic host with DI, configuration, logging, and
/// graceful shutdown handling.
/// </summary>
public static class Worker
{
    private static IHost? _host;

    internal static IHost Host =>
        _host ?? throw new InvalidOperationException("Worker host has not been started. Invoke Worker.Run(...) first.");

    /// <summary>
    /// Builds a host and runs <paramref name="work"/> as a hosted background
    /// service. The task receives the host's stopping token, which fires on
    /// Ctrl+C, SIGTERM, or any other graceful-shutdown signal.
    /// </summary>
    /// <param name="work">The async loop or one-shot body. Honor the cancellation token to shut down promptly.</param>
    /// <example>
    /// <code>
    /// await Run(async ct =>
    /// {
    ///     while (!ct.IsCancellationRequested)
    ///     {
    ///         Log.Info("tick");
    ///         await Task.Delay(TimeSpan.FromSeconds(30), ct);
    ///     }
    /// });
    /// </code>
    /// </example>
    public static Task Run(Func<CancellationToken, Task> work) =>
        Run((_, ct) => work(ct));

    /// <summary>
    /// Overload that also passes the host's <see cref="IServiceProvider"/> to
    /// <paramref name="work"/>, for scripts that resolve their own services.
    /// </summary>
    public static async Task Run(Func<IServiceProvider, CancellationToken, Task> work)
    {
        if (_host is not null)
            throw new InvalidOperationException("Worker.Run has already been invoked once in this process.");

        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Services.AddHostedService(sp => new CadenzaService(sp, work));
        _host = builder.Build();

        try
        {
            await _host.RunAsync().ConfigureAwait(false);
        }
        finally
        {
            _host = null;
        }
    }

    /// <summary>
    /// Reads a configuration value (from environment variables, <c>appsettings.json</c>,
    /// command-line, etc.) as <typeparamref name="T"/>. Throws if the key is missing.
    /// </summary>
    /// <typeparam name="T">Target type — <see cref="string"/> or any <see cref="IConvertible"/> primitive.</typeparam>
    /// <param name="key">Configuration key path, e.g. <c>"Api:Endpoint"</c>.</param>
    /// <exception cref="InvalidOperationException">Key is missing, or the host has not been started.</exception>
    public static T Config<T>(string key)
    {
        var cfg = Host.Services.GetRequiredService<IConfiguration>();
        var raw = cfg.GetSection(key).Value;
        if (raw is null)
            throw new InvalidOperationException($"Configuration key '{key}' is missing.");

        if (typeof(T) == typeof(string))
            return (T)(object)raw;

        return (T)Convert.ChangeType(raw, typeof(T), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Resolves a service of type <typeparamref name="T"/> from the host's
    /// dependency injection container.
    /// </summary>
    /// <exception cref="InvalidOperationException">No such service registered, or the host has not been started.</exception>
    public static T Service<T>() where T : notnull =>
        Host.Services.GetRequiredService<T>();

    private sealed class CadenzaService(IServiceProvider sp, Func<IServiceProvider, CancellationToken, Task> work) : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => work(sp, stoppingToken);
    }
}
