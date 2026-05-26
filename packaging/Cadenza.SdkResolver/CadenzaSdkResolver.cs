using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Build.Framework;

namespace Cadenza.SdkResolver;

/// <summary>
/// MSBuild SDK resolver that handles version-less or `@latest` references
/// to <c>Cadenza</c>, <c>Cadenza.Worker</c>, <c>Cadenza.Web</c>,
/// <c>Cadenza.Mcp</c>, etc. by querying nuget.org for the highest stable
/// version, ensuring the package is in the user's global packages folder,
/// and returning the path to the unpacked <c>Sdk/</c> directory.
///
/// Exact versions (e.g. <c>Cadenza@1.0.14</c>) are not handled here —
/// returning <c>null</c> defers to the bundled NuGet SDK resolver.
/// </summary>
internal sealed record VersionIndex([property: JsonPropertyName("versions")] string[]? Versions);

[JsonSerializable(typeof(VersionIndex))]
internal partial class VersionIndexJsonCtx : JsonSerializerContext { }

public sealed class CadenzaSdkResolver : Microsoft.Build.Framework.SdkResolver
{
    public override string Name => "Cadenza.SdkResolver";

    // The bundled NuGet resolver sits at priority 5500. Run before it so we
    // can pre-resolve `@latest` / empty versions; defer to it for concrete
    // versions by returning null.
    public override int Priority => 4500;

    private static readonly HttpClient _http = CreateHttp();

    private static HttpClient CreateHttp()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("Cadenza.SdkResolver");
        return c;
    }

    public override SdkResult? Resolve(SdkReference sdkReference, SdkResolverContext resolverContext, SdkResultFactory factory)
    {
        var name = sdkReference.Name;
        if (!IsCadenzaSdk(name)) return null;

        var requested = sdkReference.Version;
        if (HasConcreteVersion(requested)) return null;

        try
        {
            var resolved = ResolveLatestStable(name);
            if (resolved is null)
                return factory.IndicateFailure(new[] { $"Cadenza.SdkResolver: could not query nuget.org for '{name}' latest version." });

            var sdkPath = EnsurePackageOnDisk(name, resolved);
            if (sdkPath is null)
                return factory.IndicateFailure(new[] { $"Cadenza.SdkResolver: failed to download '{name}@{resolved}' from nuget.org." });

            resolverContext.Logger?.LogMessage($"Cadenza.SdkResolver resolved {name} -> {resolved}", MessageImportance.Low);
            return factory.IndicateSuccess(sdkPath, resolved);
        }
        catch (Exception ex)
        {
            return factory.IndicateFailure(new[] { $"Cadenza.SdkResolver: unexpected error resolving '{name}': {ex.GetType().Name}: {ex.Message}" });
        }
    }

    // ─── matching ──────────────────────────────────────────────────────────

    private static bool IsCadenzaSdk(string name) =>
        name.Equals("Cadenza", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("Cadenza.", StringComparison.OrdinalIgnoreCase);

    private static bool HasConcreteVersion(string? v) =>
        !string.IsNullOrWhiteSpace(v) &&
        !v.Equals("latest", StringComparison.OrdinalIgnoreCase) &&
        v != "*";

    // ─── nuget.org queries via the v3 flat container ──────────────────────

    private static string? ResolveLatestStable(string packageId)
    {
        var idLower = packageId.ToLowerInvariant();
        var url = $"https://api.nuget.org/v3-flatcontainer/{idLower}/index.json";
        using var stream = _http.GetStreamAsync(url).GetAwaiter().GetResult();
        var index = JsonSerializer.Deserialize(stream, VersionIndexJsonCtx.Default.VersionIndex);
        if (index?.Versions is null || index.Versions.Length == 0) return null;

        return index.Versions
            .Where(v => !v.Contains('-'))
            .Select(v => (Raw: v, Parsed: TryParseVersion(v)))
            .Where(t => t.Parsed is not null)
            .OrderByDescending(t => t.Parsed!)
            .Select(t => t.Raw)
            .FirstOrDefault();
    }

    private static Version? TryParseVersion(string raw)
    {
        var hyphen = raw.IndexOf('-');
        var core = hyphen >= 0 ? raw.Substring(0, hyphen) : raw;
        return Version.TryParse(core, out var v) ? v : null;
    }

    // ─── unpack into the user's global packages folder ────────────────────

    private static string? EnsurePackageOnDisk(string packageId, string version)
    {
        var globalPackages = GetGlobalPackagesFolder();
        var idLower = packageId.ToLowerInvariant();
        var packageDir = Path.Combine(globalPackages, idLower, version);
        var sdkDir = Path.Combine(packageDir, "Sdk");

        if (File.Exists(Path.Combine(sdkDir, "Sdk.props")))
            return sdkDir;

        var url = $"https://api.nuget.org/v3-flatcontainer/{idLower}/{version}/{idLower}.{version}.nupkg";
        var tmpFile = Path.Combine(Path.GetTempPath(), $"cadenza-resolver-{Guid.NewGuid():N}.nupkg");

        try
        {
            using (var src = _http.GetStreamAsync(url).GetAwaiter().GetResult())
            using (var dst = File.Create(tmpFile))
                src.CopyTo(dst);

            Directory.CreateDirectory(packageDir);
            ZipFile.ExtractToDirectory(tmpFile, packageDir, overwriteFiles: true);

            return File.Exists(Path.Combine(sdkDir, "Sdk.props")) ? sdkDir : null;
        }
        finally
        {
            try { File.Delete(tmpFile); } catch { }
        }
    }

    private static string GetGlobalPackagesFolder()
    {
        var explicit_ = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrEmpty(explicit_)) return explicit_;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".nuget", "packages");
    }

}
