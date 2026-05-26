#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.12

// Codex / Aider / Continue / Cursor backend in 25 lines.
//
// Run on the same machine as your editor and point the editor at it:
//   $env:OPENAI_BASE_URL = "http://localhost:8080/v1"   # PowerShell
//   $env:OPENAI_API_KEY  = "any-non-empty-string"
//   codex                                                # or aider, continue, ...
//
// Tools registered here are auto-callable from inside the editor's chat.
// The editor talks pure OpenAI wire format — it has no idea this is a local
// .NET process backed by Ollama.

ServedModelName = "cadenza-codex";   // shown in the editor's model picker
Port            = 8080;

SystemPrompt("""
    You are a coding assistant embedded in the user's editor. Prefer
    concrete file references and exact diffs. When the user asks about
    code, use `read_file` / `list_files` to ground your answer before
    speculating.
    """);

Tool("read_file", "Read a UTF-8 text file from the working directory",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern (e.g., src/**/*.cs)",
    (string pattern) => Glob(pattern).ToArray());

Tool("run_tests", "Run `dotnet test` and return the captured output",
    () => Capture("dotnet test --nologo"));

UseOllama("qwen2.5-coder:7b");   // or UseOpenAi("gpt-4o-mini"), etc.

await Run();
