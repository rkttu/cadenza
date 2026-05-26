#!/usr/bin/env dotnet run
#:sdk Cadenza.Web@1.0.7

// Cadenza Minimal API script. Tier 1 bare names:
//   Get/Post/Put/Delete/Map(path, handler)   — route registration
//   Run()                                    — start server
//   Web.App, Web.Services                    — escape hatches
//   ReadText/WriteText/Glob (shared)
//
// Run with:    dotnet run app.cs
// Publish to a self-contained binary:
//   dotnet publish app.cs -r linux-x64 -c Release
//
// See: https://github.com/rkttu/cadenza

Get("/", () => "Hello from Cadenza.Web");
Get("/health", () => new { status = "ok", time = DateTime.UtcNow });
Post("/echo", (EchoRequest req) => new EchoResponse(req.Message.ToUpper()));

await Run();

record EchoRequest(string Message);
record EchoResponse(string Echoed);
