#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.15

// Cadenza console script. Tier 1 bare names (no namespace prefix needed):
//   Run(cmd), Capture(cmd)         — shell exec
//   ReadText(path), WriteText(...) — UTF-8 file I/O
//   Glob(pattern), TempDir()       — file matching, disposable temp dir
//   WriteLine, Write, ReadLine     — standard System.Console
//
// Run with:    dotnet run app.cs
// Publish to a self-contained binary:
//   dotnet publish app.cs -r linux-x64 -c Release
//
// See: https://github.com/rkttu/cadenza

WriteLine("Hello from Cadenza!");
