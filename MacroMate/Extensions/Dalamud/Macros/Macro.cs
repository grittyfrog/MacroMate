using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MacroMate.Extensions.Dalamud.Macros;

/**
 * We can't use FFXIVClientLibraries Macro class directly since it's readonly.
 *
 * Instead, let's use the same approach as [QoL Bar](https://github.com/UnknownX7/QoLBar/blob/28d51789c697a1975e4472291619127b26a477b7/Structures/Macro.cs)
 */
[StructLayout(LayoutKind.Sequential, Size = 0x688)]
public readonly struct Macro : IDisposable {
    public const int numLines = 15;
    public const int size = 0x8 + (UTF8String.size * (numLines + 1));

    public readonly uint icon;
    public readonly uint key;
    public readonly UTF8String title;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = numLines)]
    public readonly UTF8String[] lines;

    public Macro(IntPtr loc, string t, IReadOnlyList<string> commands) {
        icon = 0x101D1; // 66001
        key = 1;
        title = new UTF8String(loc + 0x8, t);
        lines = new UTF8String[numLines];
        for (int i = 0; i < numLines; i++) {
            var command = (commands.Count > i) ? commands[i] : string.Empty;
            lines[i] = new UTF8String(loc + 0x8 + (UTF8String.size * (i + 1)), command);
        }
    }

    public void Dispose() {
        title.Dispose();
        for (int i = 0; i < numLines; i++) {
            lines[i].Dispose();
        }
    }
}
