using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cadenza.Web;

public static class Web
{
    private const string MapRequiresUnreferencedCode =
        "Minimal API handlers require reflection on the supplied delegate and its parameters.";
    private const string MapRequiresDynamicCode =
        "Minimal API handlers may require generated code; use source-generated routes for AOT.";

    private static WebApplicationBuilder? _builder;
    private static WebApplication? _app;

    private static WebApplicationBuilder Builder => _builder ??= WebApplication.CreateBuilder();
    public static IServiceCollection Services => Builder.Services;

    public static WebApplication App => _app ??= Builder.Build();

#pragma warning disable RDG002 // Delegate-accepting wrappers cannot be statically analyzed by the request-delegate generator.

    [RequiresUnreferencedCode(MapRequiresUnreferencedCode)]
    [RequiresDynamicCode(MapRequiresDynamicCode)]
    public static RouteHandlerBuilder Get(string path, Delegate handler) => App.MapGet(path, handler);

    [RequiresUnreferencedCode(MapRequiresUnreferencedCode)]
    [RequiresDynamicCode(MapRequiresDynamicCode)]
    public static RouteHandlerBuilder Post(string path, Delegate handler) => App.MapPost(path, handler);

    [RequiresUnreferencedCode(MapRequiresUnreferencedCode)]
    [RequiresDynamicCode(MapRequiresDynamicCode)]
    public static RouteHandlerBuilder Put(string path, Delegate handler) => App.MapPut(path, handler);

    [RequiresUnreferencedCode(MapRequiresUnreferencedCode)]
    [RequiresDynamicCode(MapRequiresDynamicCode)]
    public static RouteHandlerBuilder Delete(string path, Delegate handler) => App.MapDelete(path, handler);

    [RequiresUnreferencedCode(MapRequiresUnreferencedCode)]
    [RequiresDynamicCode(MapRequiresDynamicCode)]
    public static IEndpointConventionBuilder Map(string path, Delegate handler) => App.Map(path, handler);

#pragma warning restore RDG002

    public static Task Run() => App.RunAsync();

    public static void UseHttps() => App.UseHttpsRedirection();

    public static void UseCors(string policy = "default") => App.UseCors(policy);
}
