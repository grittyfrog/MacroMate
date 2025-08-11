using System;
using System.IO;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MacroMate.Extensions.Dalamud;

public static unsafe class ClipboardEx {
    public static Utf8String? GetPayloadEnabledClipboardString() {
        unsafe {
            var atkStage = AtkStage.Instance();
            if (atkStage == null) { return null; }

            var atkTextInput = atkStage->AtkInputManager->TextInput;
            var systemClip = atkTextInput->ClipboardData.GetSystemClipboardText();
            if (atkTextInput->CopyBufferFiltered.AsSpan().SequenceEqual(systemClip->AsSpan())) {
                return atkTextInput->CopyBufferRaw;
            }

            return *systemClip;
        }
    }

    /// <summary>
    /// Sets the clipboard text, from a <see cref="SeString"/>, payloads intact.
    /// </summary>
    /// <param name="seString">The string.</param>
    public static void SetClipboardSeString(SeString seString) {
        using var ms = new MemoryStream();
        ms.Write(seString.Encode());
        ms.WriteByte(0);
        fixed (byte* p = ms.GetBuffer())
            SetClipboardString(p);
    }

    /// <summary>
    /// Sets the clipboard text.
    /// </summary>
    /// <param name="text">The string.</param>
    /// <exception cref="ArgumentException">If text includes a null character.</exception>
    public static void SetClipboardString(ReadOnlySpan<char> text) {
        if (text.IndexOf((char)0) != -1)
            throw new ArgumentException("Cannot contain a null character (\\0).", nameof(text));

        var len = Encoding.UTF8.GetByteCount(text);
        var bytes = len <= 512 ? stackalloc byte[len + 1] : new byte[len + 1];
        Encoding.UTF8.GetBytes(text, bytes);
        bytes[len] = 0;

        fixed (byte* p = bytes)
            SetClipboardString(p);
    }

    /// <summary>
    /// Sets the clipboard text. It may include payloads; they must be correctly encoded.
    /// </summary>
    /// <param name="text">The string.</param>
    public static void SetClipboardString(byte* text) {
        ThreadSafety.AssertMainThread();

        var atkStage = AtkStage.Instance();
        if (atkStage == null) { return; }

        var atkTextInput = atkStage->AtkInputManager->TextInput;

        atkTextInput->CopyBufferRaw.SetString(text);
        atkTextInput->ClipboardData.WriteToSystemClipboard(&atkTextInput->CopyBufferRaw, &atkTextInput->CopyBufferFiltered);
    }
}
