#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.15

// Minimal AI agent. Boots an OpenAI-compatible HTTP server on
// http://localhost:8080 with two callable tools and a local Ollama backend.
//
// Point any OpenAI client at it:
//   export OPENAI_BASE_URL=http://localhost:8080/v1
//   export OPENAI_API_KEY=any-non-empty-string
//   codex      # or aider, continue, cursor, sgpt, …

SystemPrompt("You are a helpful assistant with read-only filesystem access.");

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern (e.g., **/*.cs)",
    (string pattern) => Glob(pattern).ToArray());

UseOllama("llama3.2");

await Run();
