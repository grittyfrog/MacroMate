using System;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MacroMate.Extensions.Dalamud;

public static class ClipboardEx {
    public static Utf8String? GetPayloadEnabledClipboardString() {
        unsafe {
            var atkStage = AtkStage.GetSingleton();
            if (atkStage == null) { return null; }

            var atkTextInput = atkStage->AtkInputManager->TextInput;
            var systemClip = atkTextInput->ClipboardData.GetSystemClipboardText();
            if (atkTextInput->CopyBufferFiltered.AsSpan().SequenceEqual(systemClip->AsSpan())) {
                return atkTextInput->CopyBufferRaw;
            }

            return *systemClip;
        }
    }
}
