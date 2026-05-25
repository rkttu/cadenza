using System;
using System.Runtime.InteropServices;

namespace Cadenza;

public static class Env
{
    public static string? Get(string key) => Environment.GetEnvironmentVariable(key);

    public static string Get(string key, string defaultValue) =>
        Environment.GetEnvironmentVariable(key) ?? defaultValue;

    public static string[] Args => Environment.GetCommandLineArgs();

    public static string Cwd => Environment.CurrentDirectory;

    public static void Exit(int code) => Environment.Exit(code);

    public static bool IsCi =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILDKITE")) ||
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CIRCLECI"));

    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
}
