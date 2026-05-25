using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cadenza;

public static class Fs
{
    public static string ReadText(string path) => File.ReadAllText(path, Encoding.UTF8);
    public static void WriteText(string path, string content) => File.WriteAllText(path, content, Encoding.UTF8);
    public static byte[] ReadBytes(string path) => File.ReadAllBytes(path);
    public static void WriteBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);

    public static Task<string> ReadTextAsync(string path, CancellationToken ct = default) =>
        File.ReadAllTextAsync(path, Encoding.UTF8, ct);

    public static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    public static void Delete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        else if (File.Exists(path)) File.Delete(path);
    }

    public static void Move(string src, string dst)
    {
        if (Directory.Exists(src)) Directory.Move(src, dst);
        else File.Move(src, dst, overwrite: true);
    }

    public static void Copy(string src, string dst) => File.Copy(src, dst, overwrite: true);

    public static void MakeDir(string path) => Directory.CreateDirectory(path);

    public static IEnumerable<string> Glob(string pattern)
    {
        var (baseDir, search, recursive) = SplitPattern(pattern);
        if (!Directory.Exists(baseDir)) return Array.Empty<string>();
        var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(baseDir, search, opt);
    }

    public static TempDirectory TempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "cadenza-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new TempDirectory(path);
    }

    private static (string baseDir, string search, bool recursive) SplitPattern(string pattern)
    {
        var normalized = pattern.Replace('\\', '/');
        var idx = -1;
        for (var i = 0; i < normalized.Length; i++)
        {
            var c = normalized[i];
            if (c is '*' or '?' or '[') { idx = i; break; }
        }
        if (idx < 0) return (".", normalized, false);

        var lastSlash = normalized.LastIndexOf('/', idx);
        var baseDir = lastSlash < 0 ? "." : normalized[..lastSlash];
        var rest = lastSlash < 0 ? normalized : normalized[(lastSlash + 1)..];
        var recursive = rest.StartsWith("**", StringComparison.Ordinal);
        var search = recursive
            ? (rest.Contains('/') ? rest[(rest.IndexOf('/') + 1)..] : "*")
            : rest;
        if (string.IsNullOrEmpty(search)) search = "*";
        if (string.IsNullOrEmpty(baseDir)) baseDir = ".";
        return (baseDir, search, recursive);
    }
}

public sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    internal TempDirectory(string path)
    {
        Path = path;
    }

    public override string ToString() => Path;

    public static implicit operator string(TempDirectory t) => t.Path;

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
        catch
        {
            // best effort
        }
    }
}
