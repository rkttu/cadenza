# Publishing as a single binary

> Read this in [한국어](publishing-single-binary.ko.md).

This guide applies to all four Cadenza SDK variants (`Cadenza`, `Cadenza.Worker`, `Cadenza.Web`, `Cadenza.Mcp`). The defaults are the same: a single self-contained binary, ready-to-run (R2R) compiled, with single-file compression enabled.

## Default publish

```bash
dotnet publish app.cs -r linux-x64 -c Release
```

Output: `bin/Release/net10.0/linux-x64/publish/app` — roughly 30–40 MB after compression.

Supported runtime identifiers (`-r`):

| Platform | RID |
| --- | --- |
| Linux x64 | `linux-x64` |
| Linux ARM64 | `linux-arm64` |
| macOS x64 | `osx-x64` |
| macOS Apple Silicon | `osx-arm64` |
| Windows x64 | `win-x64` |
| Windows ARM64 | `win-arm64` |

## Tuning

Disable single-file compression (faster cold start, larger binary):

```bash
dotnet publish app.cs -r linux-x64 -c Release -p:EnableCompressionInSingleFile=false
```

Bundle native libraries inside the single file (no temp extraction at runtime):

```bash
dotnet publish app.cs -r linux-x64 -c Release -p:IncludeNativeLibrariesForSelfExtract=true
```

## NativeAOT (opt-in)

Add this line to the top of your script:

```csharp
#:property PublishAot=true
```

Then publish normally:

```bash
dotnet publish app.cs -r linux-x64 -c Release
```

Output: ~10–30 MB native binary, no JIT, fastest cold start. AOT requires all dependencies to be AOT-compatible — the Cadenza core APIs are AOT-clean by construction.

## Container packaging

For Cadenza.Web, a sensible base image is `mcr.microsoft.com/dotnet/runtime-deps` since the binary is self-contained:

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0
COPY ./bin/Release/net10.0/linux-x64/publish/app /app
ENTRYPOINT ["/app"]
```

The final image lands under ~100 MB.
