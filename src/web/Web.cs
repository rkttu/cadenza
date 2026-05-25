using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cadenza.Web;

/// <summary>
/// Minimal API wrapper. Lazily constructs a <see cref="WebApplication"/> the
/// first time a route is mapped or <see cref="App"/> is accessed, so a script
/// can simply call <c>Get(...)</c>, <c>Post(...)</c>, and <c>await Run()</c>.
/// </summary>
public static class Web
{
    private const string MapRequiresUnreferencedCode =
        "Minimal API handlers require reflection on the supplied delegate and its parameters.";
    private const string MapRequiresDynamicCode =
        "Minimal API handlers may require generated code; use source-generated routes for AOT.";

    private static WebApplicationBuilder? _builder;
    private static WebApplication? _app;

    private static WebApplicationBuilder Builder => _builder ??= WebApplication.CreateBuilder();

    /// <summary>The DI service collection. Add services BEFORE <see cref="App"/> is materialized.</summary>
    public static IServiceCollection Services => Builder.Services;

    /// <summary>
    /// The underlying <see cref="WebApplication"/>. Use it directly when you
    /// need middleware, sub-paths, or other ASP.NET Core features that the
    /// shorthand wrappers don't surface.
    /// </summary>
    public static WebApplication App => _app ??= Builder.Build();

#pragma warning disable RDG002 // Delegate-accepting wrappers cannot be statically analyzed by the request-delegate generator.

    /// <summary>Map an HTTP GET endpoint.</summary>
    /// <param name="path">Route template, e.g. <c>"/users/{id}"</c>.</param>
    /// <param name="handler">Handler delegate. Parameters and return type follow Minimal API conventions.</param>
    [RequiresUnreferencedCode(MapRequiresUnreferencedCode)]
    [RequiresDynamicCode(MapRequiresDynamicCode)]
    public static RouteHandlerBuilder Get(string path, Delegate handler) => App.MapGet(path, handler);

    /// <summary>Map an HTTP POST endpoint.</summary>
    [RequiresUnreferencedCode(MapRequiresUnreferencedCode)]
    [RequiresDynamicCode(MapRequiresDynamicCode)]
    public static RouteHandlerBuilder Post(string path, Delegate handler) => App.MapPost(path, handler);

    /// <summary>Map an HTTP PUT endpoint.</summary>
    [RequiresUnreferencedCode(MapRequiresUnreferencedCode)]
    [RequiresDynamicCode(MapRequiresDynamicCode)]
    public static RouteHandlerBuilder Put(string path, Delegate handler) => App.MapPut(path, handler);

    /// <summary>Map an HTTP DELETE endpoint.</summary>
    [RequiresUnreferencedCode(MapRequiresUnreferencedCode)]
    [RequiresDynamicCode(MapRequiresDynamicCode)]
    public static RouteHandlerBuilder Delete(string path, Delegate handler) => App.MapDelete(path, handler);

    /// <summary>Map any HTTP method to <paramref name="path"/> (Minimal API <c>MapMethods</c> equivalent without a method filter).</summary>
    [RequiresUnreferencedCode(MapRequiresUnreferencedCode)]
    [RequiresDynamicCode(MapRequiresDynamicCode)]
    public static IEndpointConventionBuilder Map(string path, Delegate handler) => App.Map(path, handler);

#pragma warning restore RDG002

    /// <summary>
    /// Starts the web server and runs until <see cref="WebApplication.StopAsync(System.Threading.CancellationToken)"/>
    /// is called or the host receives a graceful-shutdown signal.
    /// </summary>
    public static Task Run() => App.RunAsync();

    /// <summary>Enable HTTPS redirection middleware.</summary>
    public static void UseHttps() => App.UseHttpsRedirection();

    /// <summary>
    /// Enable CORS using a previously registered named policy. The default
    /// argument is the conventional name <c>"default"</c>; the policy itself
    /// must be added via <see cref="Services"/> before <see cref="Run"/>.
    /// </summary>
    public static void UseCors(string policy = "default") => App.UseCors(policy);
}
