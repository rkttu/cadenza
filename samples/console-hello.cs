#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.4

// Minimal console example: list markdown files in the working directory
// with their byte size. Demonstrates Tier 1 bare names — Glob, ReadText,
// WriteLine — all without imports or namespace prefixes.

foreach (var file in Glob("**/*.md"))
{
    var content = ReadText(file);
    WriteLine($"{file}: {content.Length:N0} bytes");
}
