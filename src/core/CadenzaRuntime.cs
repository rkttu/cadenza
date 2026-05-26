using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cadenza;

internal static class CadenzaRuntime
{
    [ModuleInitializer]
    internal static void Init()
    {
        // Force UTF-8 for console I/O so CJK characters and emoji round-trip
        // correctly. On Windows the default console code page is OEM (e.g.,
        // CP949 on Korean systems, CP932 on Japanese), and `Console.WriteLine`
        // encodes strings through `Console.OutputEncoding` before writing — a
        // string already containing UTF-8 codepoints gets re-encoded into the
        // OEM page and any character outside that page becomes `?` or worse.
        //
        // Pairs with `Sh.MakeShell`, which forces the same UTF-8 encoding on
        // captured subprocess output, so the full read -> write loop stays
        // in UTF-8 end to end.
        //
        // On Linux/macOS, modern locales already default to UTF-8 so the
        // assignments are effectively no-ops; we set them anyway for parity
        // with non-UTF-8 LANG environments (LANG=C, POSIX, etc).
        // BOM-less UTF-8 — `Encoding.UTF8` would prepend `EF BB BF` to stdout,
        // which corrupts pipes (`cadenza-script | jq`, `> file`, etc.) and
        // shows up as `` characters in terminals that don't recognize a
        // BOM at the start of a stream.
        var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        try { Console.OutputEncoding = utf8NoBom; } catch { }
        try { Console.InputEncoding  = utf8NoBom; } catch { }
    }
}
