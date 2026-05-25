using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace Cadenza.Mcp;

/// <summary>
/// Ergonomic wrapper over the official <c>ModelContextProtocol</c> SDK
/// (Anthropic + Microsoft) for single-file MCP servers. Lets a script
/// register tools, resources, and prompts via lambdas and start a stdio
/// MCP server with <c>await Run()</c>.
/// </summary>
/// <remarks>
/// Cadenza.Mcp uses stdio transport, which means stdout is owned by the
/// MCP protocol. Never write to <see cref="System.Console.Out"/> — that
/// breaks the JSON-RPC stream and disconnects the client. Use <see cref="Log"/>
/// for diagnostics; it routes through <see cref="ILogger"/> to stderr.
/// </remarks>
public static class Mcp
{
    private static readonly List<McpServerTool> _tools = new();
    private static readonly List<McpServerPrompt> _prompts = new();
    private static readonly List<McpServerResource> _resources = new();
    private static IHost? _host;

    internal static IHost Host =>
        _host ?? throw new InvalidOperationException("MCP host has not been started. Invoke Mcp.Run() (or the bare-name `Run()`) first.");

    /// <summary>
    /// Registers a tool that AI clients can invoke. The handler's parameters
    /// become the tool's typed arguments and its return value is serialized
    /// as the tool result.
    /// </summary>
    /// <param name="name">Tool name (snake_case is conventional, e.g., <c>"read_file"</c>).</param>
    /// <param name="description">Plain-language description shown to the AI; this is what the model uses to decide when to call the tool.</param>
    /// <param name="handler">Sync or async delegate. Parameter and return types should be JSON-serializable.</param>
    public static void Tool(string name, string description, Delegate handler) =>
        _tools.Add(McpServerTool.Create(handler, new McpServerToolCreateOptions
        {
            Name = name,
            Description = description,
        }));

    /// <summary>
    /// Registers a resource exposed at <paramref name="uri"/>.
    /// </summary>
    /// <param name="uri">Resource URI template, e.g., <c>"file:///{path}"</c> or a fixed URI like <c>"readme://current"</c>.</param>
    /// <param name="name">Human-readable label.</param>
    /// <param name="handler">Delegate returning the resource contents (string, byte[], or a structured object).</param>
    public static void Resource(string uri, string name, Delegate handler) =>
        _resources.Add(McpServerResource.Create(handler, new McpServerResourceCreateOptions
        {
            UriTemplate = uri,
            Name = name,
        }));

    /// <summary>
    /// Registers a prompt template (note: this is the MCP <em>prompt</em> primitive,
    /// not the interactive <c>Cadenza.Prompt</c> module — the latter is intentionally
    /// unavailable in the Cadenza.Mcp variant because servers don't ask their users
    /// questions directly).
    /// </summary>
    /// <param name="name">Prompt name.</param>
    /// <param name="description">What the prompt does, surfaced to AI clients.</param>
    /// <param name="handler">Delegate that takes the user-supplied arguments and returns the formatted prompt text.</param>
    public static void Prompt(string name, string description, Delegate handler) =>
        _prompts.Add(McpServerPrompt.Create(handler, new McpServerPromptCreateOptions
        {
            Name = name,
            Description = description,
        }));

    /// <summary>
    /// Starts the MCP server on the stdio transport and runs until stdin is
    /// closed by the client or the process receives a graceful-shutdown signal.
    /// </summary>
    /// <remarks>
    /// All previously registered <see cref="Tool"/> / <see cref="Resource"/> /
    /// <see cref="Prompt"/> entries are bound at this point. Calling <c>Run</c>
    /// more than once in the same process is not supported.
    /// </remarks>
    public static async Task Run()
    {
        if (_host is not null)
            throw new InvalidOperationException("Mcp.Run has already been invoked once in this process.");

        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        // stdio MCP servers must NOT write any logger output to stdout — that
        // stream carries the JSON-RPC protocol. Route all log levels to stderr.
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

        var mcp = builder.Services.AddMcpServer().WithStdioServerTransport();
        if (_tools.Count > 0) mcp.WithTools(_tools);
        if (_prompts.Count > 0) mcp.WithPrompts(_prompts);
        if (_resources.Count > 0) mcp.WithResources(_resources);

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
}
