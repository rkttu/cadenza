using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Cadenza.Cli;

internal static partial class Program
{
    private static readonly HttpClient _http = new();
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromHours(24);

    private static async Task<int> Main(string[] args)
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("cadenza-cli");

        // Idempotent auto-bootstrap: any invocation of `cadenza` ensures the
        // SDK resolver is installed and (on Windows) the env var is set, so
        // `dotnet tool install -g Cadenza.Cli` followed by literally any
        // `cadenza` command is enough to make `dotnet run app.cs` with
        // version-less / @latest `#:sdk Cadenza` work in subsequent shells.
        //
        // Set CADENZA_SKIP_AUTOINSTALL=1 to opt out (e.g. CI, sandboxed envs).
        if (Environment.GetEnvironmentVariable("CADENZA_SKIP_AUTOINSTALL") != "1")
        {
            TryEnsureResolverInstalled();
        }

        if (args.Length == 0) return ShowHelp();

        var cmd = args[0].ToLowerInvariant();
        var rest = args.Length > 1 ? args[1..] : Array.Empty<string>();

        return cmd switch
        {
            "--version" or "-v" => ShowVersion(),
            "--help" or "-h" or "help" => ShowHelp(),
            "new" => await NewAsync(rest),
            "run" => await RunAsync(rest),
            "publish" => await PublishAsync(rest),
            "install-resolver" => InstallResolver(force: true),
            "uninstall-resolver" => UninstallResolver(),
            _ => Fail($"Unknown command: '{cmd}'. Run `cadenza help` for usage."),
        };
    }

    /// <summary>
    /// Silent, idempotent setup that runs on every <c>cadenza</c> invocation.
    /// First time: writes the resolver to the user folder, sets the env var
    /// on Windows / prints the POSIX snippet, emits a single-line notice.
    /// Subsequent times: no-op (a quick file-existence check).
    /// </summary>
    private static void TryEnsureResolverInstalled()
    {
        try
        {
            if (IsResolverFullyInstalled()) return;
            InstallResolver(force: false);
        }
        catch
        {
            // Bootstrap failures must never break the CLI; users can always
            // run `cadenza install-resolver` explicitly to surface the error.
        }
    }

    private static bool IsResolverFullyInstalled()
    {
        var dstDll = Path.Combine(ResolverInstallRoot(), "Cadenza.SdkResolver", "Cadenza.SdkResolver.dll");
        if (!File.Exists(dstDll)) return false;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var envVar = Environment.GetEnvironmentVariable("MSBUILDADDITIONALSDKRESOLVERSFOLDER", EnvironmentVariableTarget.User);
            return !string.IsNullOrEmpty(envVar);
        }

        // POSIX: we can't reliably check whether the user appended the export
        // to their shell profile, so treat the presence of the DLL as enough
        // and trust that the one-time message guided them.
        return true;
    }

    // ───────────────────────────── help / version ─────────────────────────────

    private static int ShowVersion()
    {
        var v = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(Program).Assembly.GetName().Version?.ToString()
            ?? "unknown";
        Console.WriteLine($"cadenza {v}");
        return 0;
    }

    private static int ShowHelp()
    {
        Console.WriteLine("""
            cadenza — optional CLI for the Cadenza single-file .NET scripting SDK family.

            USAGE
              cadenza new <variant> [-n <name>] [-o <dir>]
                  Scaffold a starter from Cadenza.Templates.
                  Variants: console (alias: c, cli), worker (w, svc, daemon),
                            web (api), mcp (m, server).

              cadenza run <script.cs> [<script-args>...]
                  Run a Cadenza script. If `#:sdk Cadenza@latest` or a version-less
                  `#:sdk Cadenza`, the CLI queries nuget.org for the latest stable
                  version, rewrites the script to a temp copy with the resolved
                  version, and invokes `dotnet run` on the copy. Works even
                  without the SDK resolver installed.

              cadenza publish <script.cs> [-r <rid>] [-c <config>]
                  Like `cadenza run` but forwards to `dotnet publish`.

              cadenza install-resolver
                  Install the Cadenza MSBuild SDK resolver so plain
                  `dotnet run app.cs` works with `#:sdk Cadenza` (no version)
                  or `#:sdk Cadenza@latest`. Sets the
                  MSBUILDADDITIONALSDKRESOLVERSFOLDER env var on Windows
                  automatically; prints a shell-profile snippet on macOS/Linux.

              cadenza uninstall-resolver
                  Remove the resolver and the env var (Windows).

              cadenza --version / --help

            NOTE
              This CLI is an OPTIONAL accessory. The canonical workflow is:

                  dotnet run app.cs

              which works without `cadenza` installed when the script pins an
              exact SDK version (e.g. `#:sdk Cadenza@1.0.10`). The CLI adds the
              `@latest` shortcut and convenience commands; it is not required.

            REPO  https://github.com/rkttu/cadenza
            """);
        return 0;
    }

    // ───────────────────────────── new ─────────────────────────────

    private static async Task<int> NewAsync(string[] args)
    {
        if (args.Length == 0)
            return Fail("Missing variant. Usage: cadenza new <variant> [-n <name>] [-o <dir>]");

        var variant = args[0].ToLowerInvariant();
        var template = ResolveTemplateShortName(variant);
        if (template is null)
            return Fail($"Unknown variant '{variant}'. Valid: console, worker, web, mcp (with shorthand aliases — see `cadenza help`).");

        if (!await TemplatesInstalledAsync())
        {
            Console.WriteLine("Installing Cadenza.Templates (one-time)…");
            var install = await InvokeDotnetAsync(["new", "install", "Cadenza.Templates"]);
            if (install != 0) return Fail("Failed to install Cadenza.Templates. Install manually with `dotnet new install Cadenza.Templates`.");
        }

        var dn = new List<string> { "new", template };
        dn.AddRange(args[1..]);
        return await InvokeDotnetAsync(dn);
    }

    private static string? ResolveTemplateShortName(string variant) => variant switch
    {
        "c" or "cli" or "console" => "cadenza-console",
        "w" or "svc" or "service" or "worker" or "daemon" => "cadenza-worker",
        "web" or "api" => "cadenza-web",
        "m" or "mcp" or "server" => "cadenza-mcp",
        _ => null,
    };

    private static async Task<bool> TemplatesInstalledAsync()
    {
        var psi = new ProcessStartInfo("dotnet", "new list cadenza-console")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        var stdout = await p.StandardOutput.ReadToEndAsync();
        await p.WaitForExitAsync();
        return p.ExitCode == 0 && stdout.Contains("cadenza-console");
    }

    // ───────────────────────────── run / publish ─────────────────────────────

    private static Task<int> RunAsync(string[] args) => ForwardWithRewriteAsync(args, "run");
    private static Task<int> PublishAsync(string[] args) => ForwardWithRewriteAsync(args, "publish");

    private static async Task<int> ForwardWithRewriteAsync(string[] args, string dotnetVerb)
    {
        if (args.Length == 0)
            return Fail($"Missing script path. Usage: cadenza {dotnetVerb} <script.cs> [args...]");

        var script = args[0];
        if (!File.Exists(script))
            return Fail($"Script not found: {script}");

        var (rewritten, target) = await MaybeRewriteAsync(script);
        try
        {
            var dn = new List<string> { dotnetVerb, target };
            dn.AddRange(args[1..]);
            return await InvokeDotnetAsync(dn);
        }
        finally
        {
            if (rewritten)
            {
                try { File.Delete(target); } catch { /* best effort */ }
            }
        }
    }

    private static async Task<(bool rewritten, string path)> MaybeRewriteAsync(string scriptPath)
    {
        var original = await File.ReadAllTextAsync(scriptPath);
        var (rewritten, content) = await RewriteSdkPinsAsync(original);

        if (!rewritten) return (false, scriptPath);

        var dir = Path.GetDirectoryName(Path.GetFullPath(scriptPath))!;
        var name = Path.GetFileNameWithoutExtension(scriptPath);
        var tmp = Path.Combine(dir, $".{name}.cadenza-{Guid.NewGuid():N}.cs");
        await File.WriteAllTextAsync(tmp, content);
        return (true, tmp);
    }

    private static readonly Regex _sdkPin = SdkPinRegex();
    [GeneratedRegex(@"^(\s*)#:sdk\s+(?<pkg>Cadenza(?:\.\w+)?)(?:@(?<ver>\S+))?\s*$", RegexOptions.Multiline)]
    private static partial Regex SdkPinRegex();

    private static async Task<(bool rewritten, string content)> RewriteSdkPinsAsync(string content)
    {
        var matches = _sdkPin.Matches(content);
        if (matches.Count == 0) return (false, content);

        var sb = new StringBuilder();
        var rewritten = false;
        var lastEnd = 0;

        foreach (Match m in matches)
        {
            sb.Append(content, lastEnd, m.Index - lastEnd);

            var indent = m.Groups[1].Value;
            var pkg = m.Groups["pkg"].Value;
            var ver = m.Groups["ver"].Success ? m.Groups["ver"].Value : "";

            if (ver is "" or "latest" or "*")
            {
                var resolved = await ResolveLatestAsync(pkg);
                if (resolved is null)
                {
                    sb.Append(m.Value);
                    Console.Error.WriteLine($"warning: could not resolve latest version of {pkg}; leaving the line untouched");
                }
                else
                {
                    sb.Append(indent).Append("#:sdk ").Append(pkg).Append('@').Append(resolved);
                    rewritten = true;
                }
            }
            else
            {
                sb.Append(m.Value);
            }
            lastEnd = m.Index + m.Length;
        }

        sb.Append(content, lastEnd, content.Length - lastEnd);
        return (rewritten, sb.ToString());
    }

    // ───────────────────────────── install-resolver / uninstall-resolver ─────────────────────────────

    /// <summary>
    /// The user-writable folder that holds the Cadenza SDK resolver after
    /// install. The same folder is what we point
    /// <c>MSBUILDADDITIONALSDKRESOLVERSFOLDER</c> at — MSBuild scans
    /// subfolders for &lt;Name&gt;/&lt;Name&gt;.dll + manifest pairs.
    /// </summary>
    private static string ResolverInstallRoot()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".cadenza", "sdk-resolvers");
    }

    private static int InstallResolver(bool force)
    {
        var srcDir = Path.Combine(AppContext.BaseDirectory, "cadenza-resolver");
        if (!Directory.Exists(srcDir))
        {
            if (force)
                return Fail($"Resolver payload missing — expected at '{srcDir}'. Reinstall Cadenza.Cli.");
            return 0; // silent on auto-bootstrap
        }

        var dstRoot = ResolverInstallRoot();
        var dstDir = Path.Combine(dstRoot, "Cadenza.SdkResolver");
        Directory.CreateDirectory(dstDir);

        foreach (var f in Directory.EnumerateFiles(srcDir))
        {
            var dst = Path.Combine(dstDir, Path.GetFileName(f));
            File.Copy(f, dst, overwrite: true);
        }

        // Concise output: one line on auto-bootstrap, fuller text when the
        // user explicitly invoked install-resolver.
        if (force)
            Console.WriteLine($"✓ Resolver installed at {dstDir}");
        else
            Console.Error.WriteLine($"[cadenza] First-run setup: SDK resolver installed at {dstDir}");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                Environment.SetEnvironmentVariable(
                    "MSBUILDADDITIONALSDKRESOLVERSFOLDER",
                    dstRoot,
                    EnvironmentVariableTarget.User);
                if (force)
                {
                    Console.WriteLine("✓ Set MSBUILDADDITIONALSDKRESOLVERSFOLDER user environment variable.");
                    Console.WriteLine("  Open a fresh terminal for the change to take effect.");
                }
                else
                {
                    Console.Error.WriteLine("[cadenza] Set MSBUILDADDITIONALSDKRESOLVERSFOLDER. Open a fresh terminal for version-less `#:sdk Cadenza` to work with `dotnet run`.");
                }
            }
            catch (Exception ex)
            {
                if (force) Console.Error.WriteLine($"warning: could not set user env var: {ex.Message}");
                PrintPosixInstructions(dstRoot, force);
            }
        }
        else
        {
            PrintPosixInstructions(dstRoot, force);
        }

        if (force)
        {
            Console.WriteLine();
            Console.WriteLine("Verify by running a script with a version-less #:sdk:");
            Console.WriteLine("    echo '#:sdk Cadenza' > /tmp/t.cs && echo 'WriteLine(\"ok\");' >> /tmp/t.cs");
            Console.WriteLine("    dotnet run /tmp/t.cs");
        }
        return 0;
    }

    private static void PrintPosixInstructions(string resolverRoot, bool force)
    {
        if (force)
        {
            Console.WriteLine();
            Console.WriteLine("To finish setup, add this to your shell profile:");
            Console.WriteLine();
            Console.WriteLine($"    export MSBUILDADDITIONALSDKRESOLVERSFOLDER=\"{resolverRoot}\"");
            Console.WriteLine();
            Console.WriteLine("Common profile paths: ~/.bashrc, ~/.zshrc, ~/.config/fish/config.fish");
            Console.WriteLine("Then open a fresh terminal (or `source` the file).");
        }
        else
        {
            Console.Error.WriteLine($"[cadenza] One-time setup: add to your shell profile (~/.bashrc, ~/.zshrc, …):");
            Console.Error.WriteLine($"          export MSBUILDADDITIONALSDKRESOLVERSFOLDER=\"{resolverRoot}\"");
            Console.Error.WriteLine($"          (then open a fresh terminal). Re-run `cadenza install-resolver` for the full explanation.");
        }
    }

    private static int UninstallResolver()
    {
        var dstRoot = ResolverInstallRoot();
        if (Directory.Exists(dstRoot))
        {
            try { Directory.Delete(dstRoot, recursive: true); Console.WriteLine($"✓ Removed {dstRoot}"); }
            catch (Exception ex) { Console.Error.WriteLine($"warning: could not remove {dstRoot}: {ex.Message}"); }
        }
        else
        {
            Console.WriteLine($"Resolver folder not present at {dstRoot} — nothing to remove.");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                Environment.SetEnvironmentVariable(
                    "MSBUILDADDITIONALSDKRESOLVERSFOLDER",
                    null,
                    EnvironmentVariableTarget.User);
                Console.WriteLine("✓ Cleared MSBUILDADDITIONALSDKRESOLVERSFOLDER user environment variable.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"warning: could not clear user env var: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("Remove the `export MSBUILDADDITIONALSDKRESOLVERSFOLDER=...` line from your shell profile manually.");
        }
        return 0;
    }

    // ───────────────────────────── nuget version resolution ─────────────────────────────

    private static async Task<string?> ResolveLatestAsync(string packageId)
    {
        if (TryReadCache(packageId, out var cached)) return cached;

        try
        {
            var idLower = packageId.ToLowerInvariant();
            var url = $"https://api.nuget.org/v3-flatcontainer/{idLower}/index.json";
            var idx = await _http.GetFromJsonAsync<VersionIndex>(url, VersionIndexCtx.Default.VersionIndex);
            if (idx?.Versions is null || idx.Versions.Length == 0) return null;

            var latest = idx.Versions
                .Where(v => !v.Contains('-'))
                .Select(v => (Raw: v, Parsed: ParseSimple(v)))
                .Where(t => t.Parsed is not null)
                .OrderByDescending(t => t.Parsed!)
                .FirstOrDefault();

            if (latest.Raw is null) return null;
            WriteCache(packageId, latest.Raw);
            return latest.Raw;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"warning: nuget.org query failed for {packageId}: {ex.Message}");
            return null;
        }
    }

    private static Version? ParseSimple(string raw)
    {
        var hyphen = raw.IndexOf('-');
        var core = hyphen >= 0 ? raw[..hyphen] : raw;
        return Version.TryParse(core, out var v) ? v : null;
    }

    private static bool TryReadCache(string packageId, out string? version)
    {
        version = null;
        var path = CachePath(packageId);
        if (!File.Exists(path)) return false;
        if (DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > _cacheTtl) return false;
        try { version = File.ReadAllText(path).Trim(); return !string.IsNullOrEmpty(version); }
        catch { return false; }
    }

    private static void WriteCache(string packageId, string version)
    {
        try
        {
            var path = CachePath(packageId);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, version);
        }
        catch { /* best effort */ }
    }

    private static string CachePath(string packageId)
    {
        var root = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (string.IsNullOrEmpty(root))
            root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cadenza", "cache");
        else
            root = Path.Combine(root, "cadenza");
        return Path.Combine(root, $"{packageId.ToLowerInvariant()}.version");
    }

    private sealed record VersionIndex([property: JsonPropertyName("versions")] string[]? Versions);

    [JsonSerializable(typeof(VersionIndex))]
    private partial class VersionIndexCtx : JsonSerializerContext { }

    // ───────────────────────────── helpers ─────────────────────────────

    private static async Task<int> InvokeDotnetAsync(IEnumerable<string> args)
    {
        var psi = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet");
        await p.WaitForExitAsync();
        return p.ExitCode;
    }

    private static int Fail(string msg)
    {
        Console.Error.WriteLine(msg);
        return 1;
    }
}
