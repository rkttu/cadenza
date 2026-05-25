#!/usr/bin/env dotnet run
#:sdk Cadenza@1.*

foreach (var file in Glob("**/*.md"))
{
    var content = ReadText(file);
    WriteLine($"{file}: {content.Length:N0} bytes");
}
