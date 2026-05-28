#!/usr/bin/env dotnet run
#:sdk Cadenza.Agent@1.0.15

// Console REPL — no HTTP server. Useful for quick local testing of a tool
// configuration before exposing it to editor integrations. Same `Tool(...)`
// registrations work for both `ChatLoop()` and `Run()`.
//
//   dotnet run agent-console-repl.cs

SystemPrompt("""
    You are a shell assistant. When the user asks for system information,
    use the `shell` tool with a safe read-only command. Never use destructive
    commands (rm, format, shutdown, etc.).
    """);

Tool("shell", "Run a shell command and return its stdout. Prefer read-only commands.",
    (string command) => Capture(command));

Tool("now", "Return the current local timestamp",
    () => DateTime.Now.ToString("O"));

UseOllama("llama3.2");

await ChatLoop();
