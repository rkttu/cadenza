using System;
using System.Collections.Generic;
using System.ClientModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OpenAI;
using OllamaSharp;

namespace Cadenza.Agent;

/// <summary>
/// Single-file AI agent framework. Register tools with <see cref="Tool"/>,
/// pick an LLM brain with one of the <c>Use*</c> helpers, and start the
/// agent either as an HTTP server (<see cref="Run"/>) — exposing an
/// OpenAI-compatible Chat Completion endpoint at <c>/v1/chat/completions</c>
/// so tools like Codex / Aider / Continue / Cursor can talk to it as if it
/// were OpenAI — or as a console REPL (<see cref="ChatLoop"/>) for quick
/// interactive testing.
/// </summary>
public static class Agent
{
    private static readonly List<AITool> _tools = new();
    private static IChatClient? _chatClient;
    private static IChatClient? _rawChatClient;
    private static string _systemPrompt = "You are a helpful AI assistant.";

    /// <summary>Port the HTTP server binds to. Default 8080.</summary>
    public static int Port { get; set; } = 8080;

    /// <summary>Hostname the HTTP server binds to. Default <c>localhost</c>.</summary>
    public static string HostName { get; set; } = "localhost";

    /// <summary>The model id returned by <c>/v1/models</c> and echoed in chat completions. Default <c>cadenza-agent</c>.</summary>
    public static string ServedModelName { get; set; } = "cadenza-agent";

    internal static IChatClient ChatClient =>
        _chatClient ?? throw new InvalidOperationException(
            "Configure an LLM with UseOllama / UseOpenAi / UseAnthropic / UseAzureOpenAi / UseChatClient before starting the agent.");

    /// <summary>The unwrapped <see cref="IChatClient"/> — without the
    /// function-invocation middleware. Used by the Responses-API path so
    /// client-supplied tools (e.g. Codex's <c>shell</c> / <c>apply_patch</c>)
    /// stream back to the client instead of being auto-invoked here.</summary>
    internal static IChatClient RawChatClient =>
        _rawChatClient ?? throw new InvalidOperationException(
            "Configure an LLM with UseOllama / UseOpenAi / UseAnthropic / UseAzureOpenAi / UseChatClient before starting the agent.");

    internal static IList<AITool> Tools => _tools;
    internal static string CurrentSystemPrompt => _systemPrompt;

    /// <summary>
    /// Register a callable tool the LLM can invoke. The handler's parameter
    /// names and types become the tool's argument schema; the return value
    /// (sync or async) is sent back to the LLM as the tool result.
    /// </summary>
    /// <param name="name">Tool name (snake_case is conventional).</param>
    /// <param name="description">Plain-language description used by the LLM to choose when to call this tool.</param>
    /// <param name="handler">Sync or async delegate. JSON-serializable parameter and return types are assumed.</param>
    [RequiresUnreferencedCode("AIFunctionFactory uses reflection over the delegate to bind arguments.")]
    [RequiresDynamicCode("AIFunctionFactory may generate code at runtime for argument binding.")]
    public static void Tool(string name, string description, Delegate handler) =>
        _tools.Add(AIFunctionFactory.Create(handler, new AIFunctionFactoryOptions { Name = name, Description = description }));

    /// <summary>System prompt prepended to every request. Default is a generic helpful-assistant string.</summary>
    public static void SystemPrompt(string text) => _systemPrompt = text;

    /// <summary>
    /// Use any <see cref="IChatClient"/> as the agent's brain. Wraps the
    /// supplied client with function-invocation middleware so registered
    /// tools are auto-called when the model requests them.
    /// </summary>
    public static void UseChatClient(IChatClient client)
    {
        _rawChatClient = client;
        _chatClient    = new ChatClientBuilder(client).UseFunctionInvocation().Build();
    }

    /// <summary>
    /// Use a local <a href="https://ollama.com">Ollama</a> daemon (default
    /// <c>http://localhost:11434</c>) as the LLM. Requires Ollama running and
    /// the model already pulled.
    /// </summary>
    public static void UseOllama(string model, string baseUrl = "http://localhost:11434")
    {
        var ollama = new OllamaApiClient(new Uri(baseUrl), model);
        UseChatClient(ollama);
    }

    /// <summary>
    /// Use OpenAI as the LLM. API key is taken from the <paramref name="apiKey"/>
    /// argument or the <c>OPENAI_API_KEY</c> environment variable.
    /// </summary>
    public static void UseOpenAi(string model, string? apiKey = null)
    {
        apiKey ??= Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("UseOpenAi requires an API key parameter or OPENAI_API_KEY env var.");
        var chatClient = new OpenAI.Chat.ChatClient(model, apiKey).AsIChatClient();
        UseChatClient(chatClient);
    }

    /// <summary>
    /// Use Anthropic Claude via the OpenAI-compatible endpoint at
    /// <c>https://api.anthropic.com/v1/</c>. API key from
    /// <paramref name="apiKey"/> or <c>ANTHROPIC_API_KEY</c>.
    /// </summary>
    public static void UseAnthropic(string model, string? apiKey = null)
    {
        apiKey ??= Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException("UseAnthropic requires an API key parameter or ANTHROPIC_API_KEY env var.");
        var options = new OpenAIClientOptions { Endpoint = new Uri("https://api.anthropic.com/v1/") };
        var chatClient = new OpenAI.Chat.ChatClient(model, new ApiKeyCredential(apiKey), options).AsIChatClient();
        UseChatClient(chatClient);
    }

    /// <summary>
    /// Use Azure OpenAI. Deployment name is the equivalent of "model" on
    /// Azure. API key from <paramref name="apiKey"/> or
    /// <c>AZURE_OPENAI_API_KEY</c>.
    /// </summary>
    public static void UseAzureOpenAi(string endpoint, string deploymentName, string? apiKey = null)
    {
        apiKey ??= Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")
            ?? throw new InvalidOperationException("UseAzureOpenAi requires an API key parameter or AZURE_OPENAI_API_KEY env var.");
        var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
        var chatClient = new OpenAI.Chat.ChatClient(deploymentName, new ApiKeyCredential(apiKey), options).AsIChatClient();
        UseChatClient(chatClient);
    }

    /// <summary>
    /// Start the agent as an HTTP server, exposing an OpenAI-compatible
    /// Chat Completion endpoint at <c>POST /v1/chat/completions</c> (with
    /// streaming via SSE when <c>stream=true</c>) and a list endpoint at
    /// <c>GET /v1/models</c>. Bind address controlled by <see cref="HostName"/>
    /// + <see cref="Port"/>. Tools to point external clients at this server
    /// typically set <c>OPENAI_BASE_URL=http://localhost:8080/v1</c> and
    /// <c>OPENAI_API_KEY=any-non-empty-string</c>.
    /// </summary>
    public static Task Run() => AgentServer.RunAsync();

    /// <summary>
    /// Run the agent as an interactive console REPL. Useful for ad-hoc
    /// testing without spinning up the HTTP layer. Use Ctrl+D / Ctrl+C to exit.
    /// </summary>
    public static Task ChatLoop() => AgentRepl.RunAsync();

    /// <summary>Single-shot non-interactive call — sends <paramref name="prompt"/>, returns the assistant's text response.</summary>
    public static async Task<string> Reply(string prompt)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, _systemPrompt),
            new(ChatRole.User, prompt),
        };
        var options = new ChatOptions { Tools = _tools };
        var response = await ChatClient.GetResponseAsync(messages, options).ConfigureAwait(false);
        return response.Text ?? string.Empty;
    }
}
