using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cadenza;

/// <summary>
/// File system helpers. Synchronous text/byte I/O, simple recursive globbing,
/// and a small set of path utilities tuned for scripting workloads.
/// </summary>
public static class Fs
{
    // BOM-less UTF-8 — the framework's `Encoding.UTF8` is a BOM-emitting variant
    // (3-byte `EF BB BF` preamble on writes), which corrupts files consumed by
    // strict JSON / YAML / TOML parsers (Rust serde_json, Python json, etc.).
    // Writes go through this shared instance so user scripts get the modern
    // BOM-less default; reads are unaffected because the framework's UTF-8
    // decoder transparently strips a leading BOM either way.
    private static readonly UTF8Encoding _utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Reads <paramref name="path"/> as UTF-8 text.</summary>
    /// <param name="path">File path. Relative paths resolve against <see cref="Env.Cwd"/>.</param>
    /// <returns>The entire file contents as a string.</returns>
    public static string ReadText(string path) => File.ReadAllText(path, Encoding.UTF8);

    /// <summary>Writes <paramref name="content"/> to <paramref name="path"/> as BOM-less UTF-8 text, overwriting any existing file.</summary>
    /// <param name="path">Destination file path.</param>
    /// <param name="content">Text to write.</param>
    public static void WriteText(string path, string content) => File.WriteAllText(path, content, _utf8NoBom);

    /// <summary>Reads <paramref name="path"/> as raw bytes.</summary>
    public static byte[] ReadBytes(string path) => File.ReadAllBytes(path);

    /// <summary>Writes <paramref name="bytes"/> to <paramref name="path"/>, overwriting any existing file.</summary>
    public static void WriteBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);

    /// <summary>Asynchronous version of <see cref="ReadText(string)"/>.</summary>
    public static Task<string> ReadTextAsync(string path, CancellationToken ct = default) =>
        File.ReadAllTextAsync(path, Encoding.UTF8, ct);

    /// <summary>Returns <c>true</c> if <paramref name="path"/> exists as either a file or a directory.</summary>
    public static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

    /// <summary>
    /// Deletes <paramref name="path"/>. Directories are removed recursively.
    /// Missing paths are silently ignored.
    /// </summary>
    public static void Delete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        else if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>
    /// Moves a file or directory from <paramref name="src"/> to <paramref name="dst"/>.
    /// Files overwrite an existing destination; directories require the destination to not exist.
    /// </summary>
    public static void Move(string src, string dst)
    {
        if (Directory.Exists(src)) Directory.Move(src, dst);
        else File.Move(src, dst, overwrite: true);
    }

    /// <summary>Copies a single file, overwriting <paramref name="dst"/> if it exists.</summary>
    public static void Copy(string src, string dst) => File.Copy(src, dst, overwrite: true);

    /// <summary>Creates the directory tree at <paramref name="path"/>. No-op if it already exists.</summary>
    public static void MakeDir(string path) => Directory.CreateDirectory(path);

    /// <summary>
    /// Enumerates file paths matching <paramref name="pattern"/>. Supports a leading
    /// recursive segment (<c>**</c>) and standard wildcards (<c>*</c>, <c>?</c>).
    /// </summary>
    /// <param name="pattern">
    /// Glob pattern. Examples:
    /// <list type="bullet">
    ///   <item><description><c>*.cs</c> — top-level <c>.cs</c> files in the current directory.</description></item>
    ///   <item><description><c>src/*.cs</c> — top-level under <c>src/</c>.</description></item>
    ///   <item><description><c>**/*.md</c> — all <c>.md</c> files recursively from the current directory.</description></item>
    ///   <item><description><c>docs/**/*.png</c> — all <c>.png</c> files recursively under <c>docs/</c>.</description></item>
    /// </list>
    /// </param>
    /// <returns>A lazy sequence of full paths. Empty if the base directory does not exist.</returns>
    /// <remarks>
    /// The implementation is intentionally minimal — for complex multi-segment globs
    /// (<c>src/&#x2A;&#x2A;/test/&#x2A;.cs</c>) use <see cref="System.IO.Enumeration.FileSystemEnumerable{TResult}"/>
    /// directly.
    /// </remarks>
    public static IEnumerable<string> Glob(string pattern)
    {
        var (baseDir, search, recursive) = SplitPattern(pattern);
        if (!Directory.Exists(baseDir)) return Array.Empty<string>();
        var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(baseDir, search, opt);
    }

    /// <summary>
    /// Creates a fresh temporary directory under the OS temp folder and returns
    /// a <see cref="TempDirectory"/> handle whose <see cref="IDisposable.Dispose"/>
    /// deletes the directory recursively.
    /// </summary>
    /// <example>
    /// <code>
    /// using var tmp = TempDir();
    /// WriteText(Path.Combine(tmp.Path, "data.json"), "{...}");
    /// Run($"some-tool --input {tmp.Path}");
    /// // tmp.Path is removed on scope exit
    /// </code>
    /// </example>
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

/// <summary>
/// Disposable handle to a temporary directory created by <see cref="Fs.TempDir"/>.
/// Disposing removes the directory tree on a best-effort basis.
/// </summary>
public sealed class TempDirectory : IDisposable
{
    /// <summary>Absolute path to the temporary directory.</summary>
    public string Path { get; }

    internal TempDirectory(string path)
    {
        Path = path;
    }

    /// <summary>Returns the absolute directory path.</summary>
    public override string ToString() => Path;

    /// <summary>Implicit string conversion that yields <see cref="Path"/> for use in interpolation and APIs that take a string path.</summary>
    public static implicit operator string(TempDirectory t) => t.Path;

    /// <summary>Removes the directory tree on a best-effort basis (errors are swallowed).</summary>
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
