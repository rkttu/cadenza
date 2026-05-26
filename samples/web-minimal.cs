#!/usr/bin/env dotnet run
#:sdk Cadenza.Web@1.0.12

// Minimal API in a single file. Demonstrates Tier 1 bare names — Get, Post,
// Run — plus Minimal API record binding for JSON request/response bodies.

Get("/", () => "Hello from Cadenza.Web");
Get("/health", () => new { status = "ok", time = DateTime.UtcNow });
Post("/echo", (EchoRequest req) => new EchoResponse(req.Message.ToUpper()));

await Run();

record EchoRequest(string Message);
record EchoResponse(string Echoed);
