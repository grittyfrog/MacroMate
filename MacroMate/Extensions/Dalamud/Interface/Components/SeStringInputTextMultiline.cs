using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dalamud.Str;

namespace MacroMate.Extensions.Dalamaud.Interface.Components;

public class SeStringInputTextMultiline {
    private bool processingPasteEvent = false;
    private Dictionary<string, AutoTranslatePayload> knownTranslationPayloads = new();

    public bool Draw(
        string label,
        ref SeString input,
        uint maxLength,
        Vector2 size,
        ImGuiInputTextFlags flags,
        ImGuiInputTextCallback callback
    ) {
        IndexPayloads(input);

        bool edited = false;

        flags |= ImGuiInputTextFlags.CallbackAlways;
        int? textCursorPosition = null;

        var ctrlVPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Ctrl) && ImGui.IsKeyPressed(ImGuiKey.V, false);
        var shiftInsPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Shift) && ImGui.IsKeyPressed(ImGuiKey.Insert, false);
        var isPasting = ctrlVPressed || shiftInsPressed;

        ImGuiInputTextCallback decoratedCallback;
        unsafe {
            decoratedCallback = (data) => {
                var result = callback(data);

                // We need the cursor pos for later in this function to determine where to paste to.
                textCursorPosition = data->CursorPos;

                // We supress all ImGui pasting so we can manually do it ourself later in this function.
                // This will be called once for every charcter that is pasted, and will supress them all.
                if (isPasting) {
                    data->EventChar = 0;
                    processingPasteEvent = true;
                }

                // We've finished supressing the paste events
                if (processingPasteEvent && !isPasting) {
                    var clipboardUtf8 = ClipboardEx.GetPayloadEnabledClipboardString();
                    if (clipboardUtf8 != null) {
                        var payloadEnabledClipboardText = SeString.Parse(clipboardUtf8.Value);

                        IndexPayloads(payloadEnabledClipboardText);

                        var ptr = new ImGuiInputTextCallbackDataPtr(data);
                        ptr.InsertChars(data->CursorPos, payloadEnabledClipboardText.TextValue.ReplaceLineEndings("\n"));
                        processingPasteEvent = false;
                    }
                }

                return result;
            };
        };

        var text = input.TextValue.ReplaceLineEndings("\n");
        var result = ImGui.InputTextMultiline(
            label,
            ref text,
            maxLength,
            size,
            flags,
            decoratedCallback
        );
        if (result) {
            input = SeStringEx.ParseFromText(text, knownTranslationPayloads);
            edited = true;
        }

        return edited;
    }

    private void IndexPayloads(SeString s) {
        // We need to pre-index the existing auto translate payloads in our input so we can recognise them later on
        foreach (var atPayload in s.Payloads.OfType<AutoTranslatePayload>()) {
            knownTranslationPayloads[atPayload.RawText()] = atPayload;
        }
    }
}
