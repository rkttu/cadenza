# Cadenza.Mcp

> Read this in [한국어](README.ko.md).

`Cadenza.Mcp` is the MCP-server variant of the Cadenza SDK family — a single-file scripting MSBuild SDK that wraps the official [`ModelContextProtocol`](https://github.com/modelcontextprotocol/csharp-sdk) C# SDK (maintained jointly by Anthropic and Microsoft).

## Quick start

Create a `server.cs` file:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza.Mcp@1.0.11

Tool("read_file", "Read a UTF-8 text file from disk",
    (string path) => ReadText(path));

Tool("list_files", "List files matching a glob pattern",
    (string pattern) => Glob(pattern).ToArray());

await Run();
```

Run iteratively:

```bash
dotnet run server.cs
```

Register with Claude Desktop (or another MCP client) by adding to its config:

```json
{
  "mcpServers": {
    "cadenza-files": {
      "command": "dotnet",
      "args": ["run", "/absolute/path/to/server.cs"]
    }
  }
}
```

Publish as a self-contained single binary:

```bash
dotnet publish server.cs -r linux-x64 -c Release
```

## Important: stdout is owned by the protocol

Stdio MCP servers carry JSON-RPC over stdout, so the `System.Console` bare names
(`WriteLine`, `Write`, `ReadLine`) are intentionally **not** part of the
`Cadenza.Mcp` Tier 1 surface. Writing to stdout from user code corrupts the
protocol stream and disconnects the client. Use the `Log.*` helpers for
diagnostics — they route through `ILogger` to stderr.

See the [project repository](https://github.com/rkttu/cadenza) for the full
specification, security boundary notes, and additional samples.
