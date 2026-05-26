# Troubleshooting

> Read this in [한국어](troubleshooting.ko.md).

## `#:sdk Cadenza@1.*` (wildcard / floating version) errors with "no version specified"

```text
SDK 확인자 "Microsoft.DotNet.MSBuildWorkloadSdkResolver"이(가) null을 반환했습니다.
The NuGetSdkResolver did not resolve this SDK because there was no version specified in the project or global.json.
지정された 'Cadenza/1.*' SDK를 찾을 수 없습니다.
```

Cause: **MSBuild SDK references do not support floating / wildcard versions** like `1.*` or `1.0.*`. This is a different mechanism from `PackageReference` floating versions — SDK resolution runs before NuGet restore, so the wildcard cannot be evaluated yet. NuGet treats wildcard input as "no version specified" and falls through to the error above.

### Fix: pin an exact version

Write a specific SemVer string in your script:

```csharp
#:sdk Cadenza@1.0.9
#:sdk Cadenza.Worker@1.0.9
#:sdk Cadenza.Web@1.0.9
#:sdk Cadenza.Mcp@1.0.9
```

Bump the version manually when a new release ships. The latest is on [nuget.org/packages/Cadenza](https://www.nuget.org/packages/Cadenza).

### Alternative: centralize in `global.json`

Drop a `global.json` next to (or above) your script:

```json
{
  "msbuild-sdks": {
    "Cadenza": "1.0.9",
    "Cadenza.Worker": "1.0.9",
    "Cadenza.Web": "1.0.9",
    "Cadenza.Mcp": "1.0.9"
  }
}
```

Then omit the version in scripts:

```csharp
#:sdk Cadenza
```

Useful when you have multiple scripts in the same folder and want a single place to bump.

---

## Newly-released version not picked up (stale NuGet cache)

A new version (e.g., `1.0.6`) is live on nuget.org but `dotnet run` / `dotnet build` keeps resolving to an older one:

```text
SDK 확인자 "Microsoft.DotNet.MSBuildWorkloadSdkResolver"이(가) null을 반환했습니다.
- nuget.org에서 N 버전을 찾았습니다[가장 가까운 버전: 1.0.5].
지정된 'Cadenza/1.0.6' SDK를 찾을 수 없습니다.
```

Cause: NuGet's HTTP cache keeps a previous version list snapshot. The SDK resolver consults the same cache.

### Clear only the Cadenza-related cache entries

You don't need to nuke the entire NuGet cache (`dotnet nuget locals all --clear`).

#### macOS / Linux

```bash
# 1. Drop version-list and nupkg entries from the HTTP metadata cache
find "$(dotnet nuget locals http-cache --list | awk '{print $NF}')" \
  \( -name 'list_cadenza*.dat' -o -name 'nupkg_cadenza*.dat' \) \
  -delete

# 2. Remove the extracted packages from the global packages folder
rm -rf ~/.nuget/packages/cadenza ~/.nuget/packages/cadenza.worker \
       ~/.nuget/packages/cadenza.web ~/.nuget/packages/cadenza.mcp
```

#### Windows (PowerShell)

```powershell
# 1. HTTP cache
$httpCache = (dotnet nuget locals http-cache --list).Split(' ')[-1]
Get-ChildItem -Path $httpCache -Recurse -Include 'list_cadenza*.dat','nupkg_cadenza*.dat' |
  Remove-Item -Force

# 2. Global packages folder
Remove-Item "$env:UserProfile\.nuget\packages\cadenza", `
            "$env:UserProfile\.nuget\packages\cadenza.worker", `
            "$env:UserProfile\.nuget\packages\cadenza.web", `
            "$env:UserProfile\.nuget\packages\cadenza.mcp" `
            -Recurse -Force -ErrorAction SilentlyContinue
```

The next `dotnet run` re-fetches the version index from nuget.org.

### Still stuck?

Check that your NuGet source list points at nuget.org rather than an out-of-date mirror:

```bash
dotnet nuget list source
```

`nuget.org` should be at `https://api.nuget.org/v3/index.json`. A custom mirror may lag the public index.

---

## macOS: `error MSB3552: Resource file "**/*.resx" could not be found`

Happens on Cadenza 1.0.0 or older. Fixed in 1.0.1. Clear the cache as above and pin `@1.0.1` or later.

---

## `Capture(...)` output: CJK / emoji garbled on Windows

Fixed in 1.0.4. Cadenza now decodes captured subprocess output with the host's OEM code page (CP949 on Korean Windows, CP932 on Japanese, etc.) and forces `Console.OutputEncoding = UTF-8` for write-back. Upgrade to 1.0.4+.

For the cleanest terminal rendering, run scripts inside Windows Terminal (UTF-8 by default) or run `chcp 65001` once in classic `cmd.exe` / `conhost`.
