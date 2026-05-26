#!/usr/bin/env dotnet run
#:sdk Cadenza.Mcp@1.0.13

// Cadenza MCP server script. Tier 1 bare names:
//   Tool(name, description, handler)       — register an MCP tool
//   Resource(uri, name, handler)           — register an MCP resource
//   Prompt(name, description, handler)     — register an MCP prompt template
//   Run()                                  — start MCP server on stdio
//   Log.Info/Warn/Error/Debug              — STDERR (critical — never use WriteLine)
//
// IMPORTANT: stdio MCP servers carry JSON-RPC on stdout. The SDK intentionally
// does NOT expose WriteLine as a bare name here; using it (or anything that
// writes to Console.Out) corrupts the protocol stream and disconnects the
// client. Use Log.* for diagnostics — they route to stderr.
//
// Run with:    dotnet run app.cs
// Register with Claude Desktop (mcp.json):
//   { "mcpServers": { "my-server": {
//       "command": "dotnet", "args": ["run", "/abs/path/to/app.cs"] } } }
//
// See: https://github.com/rkttu/cadenza

Tool("ping", "Health probe — returns pong",
    () => "pong");

Tool("echo", "Echo a string back to the caller",
    (string text) => text);

await Run();
