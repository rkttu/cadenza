# Cadenza

> Read this in [한국어](README.ko.md).

`Cadenza` is the console variant of the Cadenza SDK family — a single-file scripting MSBuild SDK for .NET 10+ file-based apps.

## Quick start

Create a `hello.cs` file:

```csharp
#!/usr/bin/env dotnet run
#:sdk Cadenza@1.0.9

foreach (var file in Glob("**/*.md"))
{
    var content = ReadText(file);
    WriteLine($"{file}: {content.Length:N0} bytes");
}
```

Run iteratively:

```bash
dotnet run hello.cs
```

Publish as a self-contained single binary:

```bash
dotnet publish hello.cs -r linux-x64 -c Release
```

See the [project repository](https://github.com/rkttu/cadenza) for the full specification and the Worker / Web / Mcp variants.
