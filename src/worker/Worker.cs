using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cadenza.Worker;

public static class Worker
{
    private static IHost? _host;

    internal static IHost Host =>
        _host ?? throw new InvalidOperationException("Worker host has not been started. Invoke Worker.Run(...) first.");

    public static Task Run(Func<CancellationToken, Task> work) =>
        Run((_, ct) => work(ct));

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

    public static T Service<T>() where T : notnull =>
        Host.Services.GetRequiredService<T>();

    private sealed class CadenzaService(IServiceProvider sp, Func<IServiceProvider, CancellationToken, Task> work) : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => work(sp, stoppingToken);
    }
}
