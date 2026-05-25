using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cadenza;

public static class Sh
{
    public static int Run(string cmd, bool throwOnError = false)
    {
        var psi = MakeShell(cmd, captureOutput: false);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: {cmd}");
        p.WaitForExit();
        if (throwOnError && p.ExitCode != 0)
            throw new InvalidOperationException($"Command exited with code {p.ExitCode}: {cmd}");
        return p.ExitCode;
    }

    public static string Capture(string cmd)
    {
        var psi = MakeShell(cmd, captureOutput: true);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: {cmd}");
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"Command exited with code {p.ExitCode}: {cmd}{Environment.NewLine}{stderr}");
        return stdout;
    }

    public static void Pipe(string cmd)
    {
        var psi = MakeShell(cmd, captureOutput: false);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: {cmd}");
        p.WaitForExit();
    }

    public static async Task<int> RunAsync(string cmd, CancellationToken ct = default)
    {
        var psi = MakeShell(cmd, captureOutput: false);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: {cmd}");
        await p.WaitForExitAsync(ct).ConfigureAwait(false);
        return p.ExitCode;
    }

    public static async Task<string> CaptureAsync(string cmd, CancellationToken ct = default)
    {
        var psi = MakeShell(cmd, captureOutput: true);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: {cmd}");
        var stdoutTask = p.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = p.StandardError.ReadToEndAsync(ct);
        await p.WaitForExitAsync(ct).ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"Command exited with code {p.ExitCode}: {cmd}{Environment.NewLine}{stderr}");
        return stdout;
    }

    private static ProcessStartInfo MakeShell(string cmd, bool captureOutput)
    {
        ProcessStartInfo psi;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/d /s /c \"" + cmd + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
        }
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(cmd);
        }

        psi.RedirectStandardOutput = captureOutput;
        psi.RedirectStandardError = captureOutput;
        return psi;
    }
}
