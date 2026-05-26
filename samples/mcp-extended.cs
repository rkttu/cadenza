#!/usr/bin/env dotnet run
#:sdk Cadenza.Mcp@1.0.11

// MCP server with Tool, Resource, and Prompt primitives. Demonstrates:
//   - Tool with external API call (reads env var for API key)
//   - Resource exposing a known file as a fixed URI
//   - Prompt template for code review
//   - Log.Info to stderr (never use WriteLine in an MCP stdio server — it
//     corrupts the JSON-RPC stream on stdout)

Tool("get_weather", "Get current weather for a city",
    async (string city) =>
    {
        var key = Env.Get("OPENWEATHER_API_KEY")
            ?? throw new InvalidOperationException("OPENWEATHER_API_KEY env var missing");
        var url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={key}";
        return await Http.GetText(url);
    });

Resource("readme://current", "Project README",
    () => ReadText("README.md"));

Prompt("review_code", "Review the given code for issues",
    (string code) => $"Please review this code for bugs, security, and style:\n\n{code}");

Log.Info("Cadenza.Mcp server starting on stdio");
await Run();
