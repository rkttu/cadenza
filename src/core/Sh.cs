using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cadenza;

public static class Sh
{
    private static readonly Encoding _captureEncoding = ResolveCaptureEncoding();

    private static Encoding ResolveCaptureEncoding()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Encoding.UTF8;

        // On Windows, cmd.exe writes its translated UI strings (e.g., date
        // labels, drive labels) in the OEM code page set at process start,
        // regardless of any later `chcp` change. Read with the same OEM page
        // so CJK and other non-ASCII characters round-trip through Capture.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try
        {
            var oemCp = CultureInfo.CurrentCulture.TextInfo.OEMCodePage;
            if (oemCp == 0) oemCp = 437;
            return Encoding.GetEncoding(oemCp);
        }
        catch
        {
            return Encoding.UTF8;
        }
    }

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

        if (captureOutput)
        {
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            // Decode with the encoding the child actually writes:
            //  - Windows: the OEM code page cmd.exe uses for its translated
            //    UI strings (CP949 on Korean Windows, CP932 on Japanese, etc.)
            //  - POSIX: UTF-8 (modern locale default)
            psi.StandardOutputEncoding = _captureEncoding;
            psi.StandardErrorEncoding = _captureEncoding;
        }
        return psi;
    }
}
