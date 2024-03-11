using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

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
                    SeString? payloadEnabledClipboardText = null;
                    unsafe {
                        var atkStage = AtkStage.GetSingleton();
                        if (atkStage != null) {
                            var clipboardText = atkStage->AtkInputManager->TextInput->CopyBufferRaw;
                            payloadEnabledClipboardText = SeString.Parse(clipboardText);
                        }
                    }

                    if (payloadEnabledClipboardText != null) {
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
            var chunks = SeStringChunk.Of(text);
            var payloads = chunks.Select(chunk => FromChunk(chunk)).ToList();
            var seString = new SeString(payloads);
            input = seString;
            edited = true;
        }

        return edited;
    }

    private void IndexPayloads(SeString s) {
        // We need to pre-index the existing auto translate payloads in our input so we can recognise them later on
        foreach (var atPayload in s.Payloads.OfType<AutoTranslatePayload>()) {
            knownTranslationPayloads[atPayload.Text] = atPayload;
        }
    }

    private Payload FromChunk(SeStringChunk chunk) {
        switch (chunk) {
            case SeStringChunk.Text textChunk: return new TextPayload(textChunk.text);
            case SeStringChunk.AutoTranslate atChunk:
                if (knownTranslationPayloads.TryGetValue(atChunk.activeLanguageText, out var atPayload)) {
                    return atPayload;
                } else {
                    return new TextPayload(atChunk.activeLanguageText);
                }
            default: throw new Exception("Unrecognised chunk type");
        }
    }

    internal interface SeStringChunk {
        public record class Text(string text) : SeStringChunk {}
        public record class AutoTranslate(string activeLanguageText) : SeStringChunk {}

        private static readonly char AutoTranslateStart = '\uE040';
        private static readonly char AutoTranslateEnd = '\uE041';

        private static readonly Parser<char, char> autoTranslateStart = Char(AutoTranslateStart);
        private static readonly Parser<char, char> autoTranslateEnd = Char(AutoTranslateEnd);

        private static readonly Parser<char, SeStringChunk> autoTranslatePayload =
            Token(c => c != AutoTranslateEnd)
                .ManyString()
                .Between(Char(AutoTranslateStart), Char(AutoTranslateEnd))
                .Select<SeStringChunk>(s => new SeStringChunk.AutoTranslate(AutoTranslateStart + s + AutoTranslateEnd));

        private static readonly Parser<char, SeStringChunk> textPayload =
            Token(c => c != AutoTranslateStart)
                .AtLeastOnceString()
                .Select<SeStringChunk>(s => new SeStringChunk.Text(s.ToString()));

        private static readonly Parser<char, SeStringChunk> payload = Try(autoTranslatePayload).Or(textPayload);

        private static readonly Parser<char, IEnumerable<SeStringChunk>> seStringPayloadParser =
            payload.Many();

        public static List<SeStringChunk> Of(string text) =>
            payload.Many().ParseOrThrow(text).ToList();
    }
}
