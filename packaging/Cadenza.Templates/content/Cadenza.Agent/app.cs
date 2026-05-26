#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.12

// Cadenza AI agent script. Tier 1 bare names:
//   Tool(name, description, handler)       — register a callable tool
//   SystemPrompt(text)                     — override the default system prompt
//   UseOllama / UseOpenAi / UseAnthropic / UseAzureOpenAi / UseChatClient
//                                          — pick the LLM brain
//   Run()                                  — start OpenAI-compatible HTTP server
//                                            on http://localhost:8080 by default
//   ChatLoop()                             — interactive console REPL instead
//   Reply(prompt)                          — one-shot non-interactive call
//   Port, HostName, ServedModelName        — server config (set before Run)
//
// Run with:    dotnet run app.cs
//
// Point any OpenAI-compatible client at the server:
//   export OPENAI_BASE_URL=http://localhost:8080/v1
//   export OPENAI_API_KEY=any-non-empty-string
//   codex      # or aider, continue, cursor, sgpt, …
//
// See: https://github.com/rkttu/cadenza

SystemPrompt("You are a helpful assistant with filesystem access.");

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern (e.g. **/*.cs)",
    (string pattern) => Glob(pattern).ToArray());

// Pick your LLM. Defaults shown — uncomment / swap as needed.
UseOllama("llama3.2");
// UseOpenAi("gpt-4o-mini");
// UseAnthropic("claude-3-5-sonnet-latest");
// UseAzureOpenAi("https://my-resource.openai.azure.com/", "my-deployment");

await Run();
