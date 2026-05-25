using System;
using System.Text;

namespace Cadenza;

/// <summary>
/// Interactive console prompts. Each method falls back to a non-interactive
/// path when the script runs in CI (see <see cref="Env.IsCi"/>) or when the
/// caller has pre-set <c>CADENZA_PROMPT_&lt;NAME&gt;</c> in the environment,
/// where <c>&lt;NAME&gt;</c> is the question text uppercased with non-alphanumeric
/// characters replaced by <c>_</c>.
/// </summary>
public static class Prompt
{
    /// <summary>
    /// Yes/no question. In CI/non-interactive mode, reads the
    /// <c>CADENZA_PROMPT_&lt;NAME&gt;</c> environment variable (parsing
    /// <c>y/yes/true/1</c> as true, <c>n/no/false/0</c> as false) and falls
    /// back to <paramref name="defaultValue"/> otherwise.
    /// </summary>
    /// <param name="question">Prompt text shown to the user.</param>
    /// <param name="defaultValue">Value returned for an empty answer or when CI defaults apply.</param>
    public static bool Confirm(string question, bool defaultValue = false)
    {
        if (TryNonInteractive(question, out var injected))
            return ParseBool(injected, defaultValue);

        var hint = defaultValue ? "[Y/n]" : "[y/N]";
        Console.Write($"{question} {hint} ");
        var line = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(line)) return defaultValue;
        return ParseBool(line, defaultValue);
    }

    /// <summary>
    /// Choose one of <paramref name="options"/>. Returns the zero-based index.
    /// In CI mode, the <c>CADENZA_PROMPT_&lt;NAME&gt;</c> variable must be set
    /// to the desired index; otherwise an <see cref="InvalidOperationException"/>
    /// is thrown.
    /// </summary>
    /// <param name="question">Prompt text.</param>
    /// <param name="options">Choices shown one per line, numbered from 1.</param>
    /// <returns>Zero-based index of the selected option.</returns>
    public static int Select(string question, string[] options)
    {
        if (options.Length == 0)
            throw new ArgumentException("options must contain at least one entry", nameof(options));

        if (TryNonInteractive(question, out var injected))
        {
            if (int.TryParse(injected, out var idx) && idx >= 0 && idx < options.Length)
                return idx;
            throw new InvalidOperationException(
                $"Prompt.Select '{question}' requires CADENZA_PROMPT_{SafeKey(question)}=<index 0..{options.Length - 1}> in non-interactive mode.");
        }

        Console.WriteLine(question);
        for (var i = 0; i < options.Length; i++)
            Console.WriteLine($"  {i + 1}. {options[i]}");
        while (true)
        {
            Console.Write("Select: ");
            var line = Console.ReadLine();
            if (int.TryParse(line, out var n) && n >= 1 && n <= options.Length)
                return n - 1;
            Console.WriteLine("Invalid selection.");
        }
    }

    /// <summary>
    /// Free-form text answer. In CI mode reads <c>CADENZA_PROMPT_&lt;NAME&gt;</c>,
    /// falls back to <paramref name="defaultValue"/>, or throws if neither is available.
    /// </summary>
    /// <param name="question">Prompt text.</param>
    /// <param name="defaultValue">Returned for an empty answer or as the CI fallback when no env var is set.</param>
    public static string Text(string question, string? defaultValue = null)
    {
        if (TryNonInteractive(question, out var injected))
            return injected;

        if (Env.IsCi)
            return defaultValue
                ?? throw new InvalidOperationException(
                    $"Prompt.Text '{question}' requires CADENZA_PROMPT_{SafeKey(question)} or a defaultValue in non-interactive mode.");

        var hint = defaultValue is null ? "" : $" [{defaultValue}]";
        Console.Write($"{question}{hint}: ");
        var line = Console.ReadLine();
        if (string.IsNullOrEmpty(line))
            return defaultValue ?? "";
        return line;
    }

    /// <summary>
    /// Password-style prompt: typed characters are not echoed. In CI mode
    /// reads <c>CADENZA_PROMPT_&lt;NAME&gt;</c>; if unset, throws (passwords
    /// have no safe default).
    /// </summary>
    public static string Password(string question)
    {
        if (TryNonInteractive(question, out var injected))
            return injected;

        if (Env.IsCi)
            throw new InvalidOperationException(
                $"Prompt.Password '{question}' requires CADENZA_PROMPT_{SafeKey(question)} in non-interactive mode.");

        Console.Write($"{question}: ");
        var sb = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); break; }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0) sb.Length--;
                continue;
            }
            if (!char.IsControl(key.KeyChar)) sb.Append(key.KeyChar);
        }
        return sb.ToString();
    }

    private static bool TryNonInteractive(string question, out string value)
    {
        var v = Environment.GetEnvironmentVariable("CADENZA_PROMPT_" + SafeKey(question));
        if (v is not null)
        {
            value = v;
            return true;
        }
        value = "";
        return false;
    }

    private static string SafeKey(string q)
    {
        var sb = new StringBuilder(q.Length);
        foreach (var c in q.ToUpperInvariant())
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        return sb.ToString();
    }

    private static bool ParseBool(string s, bool defaultValue)
    {
        s = s.Trim().ToLowerInvariant();
        return s switch
        {
            "y" or "yes" or "true" or "1" => true,
            "n" or "no" or "false" or "0" => false,
            _ => defaultValue,
        };
    }
}
