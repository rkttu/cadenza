using System;
using System.Text;

namespace Cadenza;

public static class Prompt
{
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
