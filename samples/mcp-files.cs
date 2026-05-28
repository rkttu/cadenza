#!/usr/bin/env dotnet run
#:sdk Cadenza.Mcp@1.0.15

// Minimal MCP server exposing file read / glob tools to AI clients.
// Register with Claude Desktop / Cursor / VS Code MCP by pointing the
// client config at `dotnet run /absolute/path/to/this/file.cs`.

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern (e.g., **/*.cs)",
    (string pattern) => Glob(pattern).ToArray());

await Run();
