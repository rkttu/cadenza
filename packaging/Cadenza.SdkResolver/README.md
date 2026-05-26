# Cadenza.SdkResolver

> Read this in [한국어](README.ko.md).

**Optional, opt-in** MSBuild SDK resolver for the Cadenza single-file scripting SDK family. Installing it lets `dotnet run app.cs` and `dotnet build` accept:

```csharp
#:sdk Cadenza               // no version — resolved to the latest stable on nuget.org
#:sdk Cadenza@latest        // explicit "latest" alias
#:sdk Cadenza@*             // wildcard alias
```

The canonical Cadenza workflow does NOT require this package — `dotnet run app.cs` with an exact pinned version (`#:sdk Cadenza@1.0.12`) already works without any extra install. Install this resolver only if you specifically want the version-less / `@latest` shorthand in scripts where reproducibility is not critical (quick experiments, REPL-style throwaway code).

For production scripts you want to pin an exact version anyway. See the [project repository](https://github.com/rkttu/cadenza) for the broader story.

## How it fits into MSBuild

MSBuild walks a chain of SDK resolvers in priority order. The bundled NuGet resolver activates only when a version is specified. This resolver runs at priority 4500 (before NuGet at 5500), recognizes any `Cadenza*` SDK reference with an empty / `latest` / `*` version, queries `https://api.nuget.org/v3-flatcontainer/<id>/index.json` for the highest stable SemVer, downloads the matching nupkg into `~/.nuget/packages/`, and returns the path to its `Sdk/` directory. Concrete versions (e.g. `Cadenza@1.0.12`) are deferred to the NuGet resolver via a `null` return.

## Install

There is intentionally no automated installer — the resolver requires writing the assembly to a specific location and setting one environment variable. Use whichever path fits your workflow:

### macOS / Linux

```bash
PKG=$(curl -fsSL https://api.nuget.org/v3-flatcontainer/cadenza.sdkresolver/index.json | \
      grep -oE '"[^"]+"' | grep -vE '\-' | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' | sort -V | tail -1)
TMP=$(mktemp -d)
curl -fsSL "https://api.nuget.org/v3-flatcontainer/cadenza.sdkresolver/$PKG/cadenza.sdkresolver.$PKG.nupkg" -o "$TMP/pkg.nupkg"
unzip -q "$TMP/pkg.nupkg" 'tools/net10.0/*' -d "$TMP/x"
DEST="$HOME/.cadenza/sdk-resolvers/Cadenza.SdkResolver"
mkdir -p "$DEST"
cp "$TMP/x/tools/net10.0/Cadenza.SdkResolver.dll" "$DEST/"
cp "$TMP/x/tools/net10.0/Cadenza.SdkResolver.xml" "$DEST/"
rm -rf "$TMP"
echo 'export MSBUILDADDITIONALSDKRESOLVERSFOLDER="$HOME/.cadenza/sdk-resolvers"' >> ~/.bashrc  # or ~/.zshrc, ~/.profile
```

Then open a fresh terminal.

### Windows (PowerShell)

```powershell
$idx = Invoke-RestMethod 'https://api.nuget.org/v3-flatcontainer/cadenza.sdkresolver/index.json'
$ver = $idx.versions | Where-Object { $_ -notmatch '-' } | Sort-Object {[Version]$_} | Select-Object -Last 1
$tmp = New-Item -ItemType Directory -Force "$env:Temp\cadenza-resolver-$([guid]::NewGuid().ToString('N'))"
Invoke-WebRequest "https://api.nuget.org/v3-flatcontainer/cadenza.sdkresolver/$ver/cadenza.sdkresolver.$ver.nupkg" -OutFile "$tmp\pkg.nupkg"
Expand-Archive "$tmp\pkg.nupkg" -DestinationPath "$tmp\x" -Force
$dest = "$env:UserProfile\.cadenza\sdk-resolvers\Cadenza.SdkResolver"
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item "$tmp\x\tools\net10.0\Cadenza.SdkResolver.dll" $dest -Force
Copy-Item "$tmp\x\tools\net10.0\Cadenza.SdkResolver.xml" $dest -Force
Remove-Item $tmp -Recurse -Force
[Environment]::SetEnvironmentVariable('MSBUILDADDITIONALSDKRESOLVERSFOLDER', "$env:UserProfile\.cadenza\sdk-resolvers", 'User')
```

Then open a fresh terminal.

## Uninstall

Remove the resolver folder and clear the env var:

```bash
# POSIX
rm -rf ~/.cadenza/sdk-resolvers
# then delete the export line from your shell profile
```

```powershell
# Windows
Remove-Item "$env:UserProfile\.cadenza\sdk-resolvers" -Recurse -Force
[Environment]::SetEnvironmentVariable('MSBUILDADDITIONALSDKRESOLVERSFOLDER', $null, 'User')
```

## Trade-offs you are opting into

- **Non-determinism.** `Cadenza@latest` resolves to a different version over time. Two `dotnet run` invocations weeks apart may compile against different SDKs. Pin an exact version for any script you intend to publish or share.
- **Network at evaluation time.** The resolver queries nuget.org once per invocation (uncached). Offline / restricted-network environments will fall through to the bundled NuGet resolver, which then fails with the standard "no version specified" error.
- **Public feed only.** The resolver hits nuget.org directly; it doesn't honor private feeds or `nuget.config`-configured sources for the version lookup step (it does use the user's global packages folder for caching the extracted SDK).

See [the main repository](https://github.com/rkttu/cadenza) for the broader Cadenza family.
