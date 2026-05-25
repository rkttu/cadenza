#!/usr/bin/env dotnet run
#:sdk Cadenza.Web@1.0.1

Get("/", () => "Hello from Cadenza.Web");
Get("/health", () => new { status = "ok", time = DateTime.UtcNow });
Post("/echo", (EchoRequest req) => new EchoResponse(req.Message.ToUpper()));

await Run();

record EchoRequest(string Message);
record EchoResponse(string Echoed);
