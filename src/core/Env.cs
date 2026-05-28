using System;
using System.Runtime.InteropServices;

namespace Cadenza;

/// <summary>
/// Environment, process, and platform helpers. Wraps <see cref="System.Environment"/>
/// and <see cref="RuntimeInformation"/> with shorter scripting-friendly names.
/// </summary>
public static class Env
{
    /// <summary>Returns the environment variable named <paramref name="key"/>, or <c>null</c> if unset.</summary>
    public static string? Get(string key) => Environment.GetEnvironmentVariable(key);

    /// <summary>Returns the environment variable named <paramref name="key"/>, or <paramref name="defaultValue"/> if unset.</summary>
    public static string Get(string key, string defaultValue) =>
        Environment.GetEnvironmentVariable(key) ?? defaultValue;

    /// <summary>
    /// All command-line tokens including the host executable as element 0.
    /// For file-based programs that means the dotnet host path, then the
    /// arguments after the script path.
    /// </summary>
    public static string[] Args => Environment.GetCommandLineArgs();

    /// <summary>Current working directory.</summary>
    public static string Cwd => Environment.CurrentDirectory;

    /// <summary>
    /// Absolute path to the entry-point <c>.cs</c> script when the program is
    /// launched via <c>dotnet run app.cs</c>. The .NET 10+ file-based program
    /// CLI injects this through a synthesized <c>RuntimeHostConfigurationOption</c>
    /// item that surfaces via <see cref="AppContext.GetData(string)"/>.
    /// </summary>
    /// <remarks>
    /// Returns <c>null</c> when the value is not available — notably after
    /// <c>dotnet publish</c>, where the CLI strips the directive (the binary
    /// is no longer "the script" so a path back to the source would lie).
    /// Reach for <see cref="Cwd"/> or <see cref="System.Reflection.Assembly.GetEntryAssembly"/>
    /// in those cases.
    /// </remarks>
    public static string? ScriptPath => AppContext.GetData("EntryPointFilePath") as string;

    /// <summary>
    /// Directory containing the entry-point <c>.cs</c> script. Same lifetime
    /// caveats as <see cref="ScriptPath"/> — <c>null</c> outside <c>dotnet run</c>
    /// / <c>dotnet build</c>.
    /// </summary>
    public static string? ScriptDirectory => AppContext.GetData("EntryPointFileDirectoryPath") as string;

    /// <summary>Terminates the current process with the given exit code. Does not return.</summary>
    public static void Exit(int code) => Environment.Exit(code);

    /// <summary>
    /// Heuristic detection of common CI environments. Checks <c>CI</c>,
    /// <c>GITHUB_ACTIONS</c>, <c>TF_BUILD</c>, <c>BUILDKITE</c>, <c>CIRCLECI</c>.
    /// Used by <see cref="Prompt"/> to fall back to defaults / environment
    /// variables instead of blocking on stdin.
    /// </summary>
    public static bool IsCi =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILDKITE")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CIRCLECI"));

    /// <summary><c>true</c> when running on Windows.</summary>
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <summary><c>true</c> when running on macOS.</summary>
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <summary><c>true</c> when running on Linux.</summary>
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}
