#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.13

// Pick the LLM backend at startup based on an env var. Useful for switching
// between local Ollama (dev) and a hosted model (CI / prod) without changing
// the agent code. Tools and system prompt stay identical.
//
//   $env:LLM_BACKEND = "openai"; $env:OPENAI_API_KEY = "sk-..."; dotnet run agent-multi-llm.cs
//   $env:LLM_BACKEND = "anthropic"; $env:ANTHROPIC_API_KEY = "sk-..."; dotnet run agent-multi-llm.cs
//   $env:LLM_BACKEND = "ollama"; dotnet run agent-multi-llm.cs

SystemPrompt("You are a helpful assistant.");

Tool("today", "Return today's local date",
    () => DateTime.Now.ToString("yyyy-MM-dd"));

Tool("uptime", "Return how long the agent process has been running, in seconds",
    () =>
    {
        using var p = System.Diagnostics.Process.GetCurrentProcess();
        return (DateTime.Now - p.StartTime).TotalSeconds;
    });

switch ((Env.Get("LLM_BACKEND") ?? "ollama").ToLowerInvariant())
{
    case "openai":
        UseOpenAi(Env.Get("OPENAI_MODEL") ?? "gpt-4o-mini");
        break;
    case "anthropic":
        UseAnthropic(Env.Get("ANTHROPIC_MODEL") ?? "claude-3-5-sonnet-latest");
        break;
    case "azure":
        UseAzureOpenAi(
            Env.Get("AZURE_OPENAI_ENDPOINT")   ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT missing"),
            Env.Get("AZURE_OPENAI_DEPLOYMENT") ?? throw new InvalidOperationException("AZURE_OPENAI_DEPLOYMENT missing"));
        break;
    default:
        UseOllama(Env.Get("OLLAMA_MODEL") ?? "llama3.2");
        break;
}

await Run();
