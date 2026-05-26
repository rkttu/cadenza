#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.14

// Count source files by extension under the current directory.
// Demonstrates: Glob recursion, basic LINQ grouping, Path helpers, sorted output.

var counts = Glob("**/*.*")
    .Select(p => Path.GetExtension(p).ToLowerInvariant())
    .Where(ext => ext.Length > 0)
    .GroupBy(ext => ext)
    .Select(g => (Ext: g.Key, Count: g.Count(), Bytes: g.Sum(_ => 0L)))
    .OrderByDescending(t => t.Count);

WriteLine($"{"Extension",-12} {"Files",8}");
WriteLine(new string('-', 22));
foreach (var (ext, count, _) in counts.Take(20))
    WriteLine($"{ext,-12} {count,8:N0}");
