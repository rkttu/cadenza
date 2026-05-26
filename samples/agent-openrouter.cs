#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.13

// OpenRouter backend — one API, hundreds of models (OpenAI, Anthropic, Google,
// Meta, Mistral, …) plus per-request fallback. OpenRouter speaks the OpenAI
// Chat Completion wire format, so we plug it in via `UseChatClient` with a
// custom OpenAI ChatClient pointed at https://openrouter.ai/api/v1.
//
// Setup:
//   $env:OPENROUTER_API_KEY = "sk-or-v1-..."
//   dotnet run agent-openrouter.cs
//
// Switch models without touching the agent:
//   $env:OPENROUTER_MODEL = "anthropic/claude-3.5-sonnet"
//   $env:OPENROUTER_MODEL = "openai/gpt-4o-mini"
//   $env:OPENROUTER_MODEL = "meta-llama/llama-3.1-70b-instruct"
//   $env:OPENROUTER_MODEL = "google/gemini-2.0-flash-exp:free"
//
// Browse the model catalog: https://openrouter.ai/models

using System.ClientModel;
using OpenAI;

var apiKey = Env.Get("OPENROUTER_API_KEY")
    ?? throw new InvalidOperationException("OPENROUTER_API_KEY env var missing — get one at https://openrouter.ai/keys");
var model  = Env.Get("OPENROUTER_MODEL") ?? "openai/gpt-4o-mini";

SystemPrompt("You are a helpful assistant with read-only filesystem access.");

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern (e.g., **/*.cs)",
    (string pattern) => Glob(pattern).ToArray());

var openAiOptions = new OpenAIClientOptions { Endpoint = new Uri("https://openrouter.ai/api/v1") };
var chatClient    = new OpenAI.Chat.ChatClient(model, new ApiKeyCredential(apiKey), openAiOptions)
    .AsIChatClient();

UseChatClient(chatClient);

ServedModelName = $"openrouter:{model}";    // shows up in editor model pickers

await Run();
