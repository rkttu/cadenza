using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cadenza;

/// <summary>
/// Shell execution helpers. Commands are dispatched through the platform's
/// default shell (<c>cmd.exe /d /s /c</c> on Windows, <c>/bin/sh -c</c>
/// elsewhere) so shell metacharacters (<c>|</c>, <c>&amp;&amp;</c>, <c>&gt;</c>)
/// behave as the user expects.
/// </summary>
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

    /// <summary>
    /// Runs <paramref name="cmd"/> and waits for it to complete, inheriting
    /// stdout/stderr from the caller's terminal.
    /// </summary>
    /// <param name="cmd">Shell command line (interpreted by cmd.exe or /bin/sh).</param>
    /// <param name="throwOnError">
    /// When <c>true</c>, throws <see cref="InvalidOperationException"/> if the
    /// process exits with a non-zero status. Default is <c>false</c> — the
    /// caller inspects the returned exit code.
    /// </param>
    /// <returns>The process exit code.</returns>
    /// <example>
    /// <code>
    /// if (Run("dotnet test") != 0)
    ///     Env.Exit(1);
    /// </code>
    /// </example>
    public static int Run(string cmd, bool throwOnError = false)
    {
        var psi = MakeShell(cmd, captureOutput: false);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: {cmd}");
        p.WaitForExit();
        if (throwOnError && p.ExitCode != 0)
            throw new InvalidOperationException($"Command exited with code {p.ExitCode}: {cmd}");
        return p.ExitCode;
    }

    /// <summary>
    /// Runs <paramref name="cmd"/> and returns its captured standard output.
    /// </summary>
    /// <param name="cmd">Shell command line.</param>
    /// <returns>The full stdout text produced by the command.</returns>
    /// <remarks>
    /// On Windows the output is decoded with the host's OEM code page so CJK
    /// characters and other non-ASCII text from <c>dir</c>, <c>git</c>, etc.
    /// round-trip correctly. On Linux/macOS UTF-8 is used.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the process exits with a non-zero status. The exception
    /// message includes captured stderr to aid diagnosis.
    /// </exception>
    /// <example>
    /// <code>
    /// var branch = Capture("git rev-parse --abbrev-ref HEAD").Trim();
    /// WriteLine($"Branch: {branch}");
    /// </code>
    /// </example>
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

    /// <summary>
    /// Runs <paramref name="cmd"/> with stdout/stderr inherited from the
    /// caller's terminal. Identical to <see cref="Run(string, bool)"/> with
    /// <c>throwOnError=false</c>, except the exit code is discarded.
    /// </summary>
    /// <param name="cmd">Shell command line.</param>
    public static void Pipe(string cmd)
    {
        var psi = MakeShell(cmd, captureOutput: false);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: {cmd}");
        p.WaitForExit();
    }

    /// <summary>
    /// Asynchronous version of <see cref="Run(string, bool)"/>; does not
    /// throw on non-zero exit.
    /// </summary>
    /// <param name="cmd">Shell command line.</param>
    /// <param name="ct">Cancellation token. Cancelling does not kill the child process; it only stops waiting.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(string cmd, CancellationToken ct = default)
    {
        var psi = MakeShell(cmd, captureOutput: false);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start: {cmd}");
        await p.WaitForExitAsync(ct).ConfigureAwait(false);
        return p.ExitCode;
    }

    /// <summary>
    /// Asynchronous version of <see cref="Capture(string)"/>.
    /// </summary>
    /// <param name="cmd">Shell command line.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The full stdout text produced by the command.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the process exits with a non-zero status.
    /// </exception>
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
