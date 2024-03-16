using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ImGuiNET;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dalamud.Str;
using MacroMate.Extensions.Imgui;

namespace MacroMate.Extensions.Dalamaud.Interface.Components;

public class SeStringInputTextMultiline {
    private bool processingPasteEvent = false;
    private Dictionary<string, AutoTranslatePayload> knownTranslationPayloads = new();
    private InputTextDecorator textDecorator = new();

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


        var inputCopy = input;
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

                // If the selection overlaps a AutoTranslateText, increase the selection so the entire payload is
                // selected (similar to what the in-game macro window does)
                var (adjSelectStart, adjSelectEnd) = ExpandSelectionAcrossAutoTranslate(inputCopy, data->SelectionStart, data->SelectionEnd);
                data->SelectionStart = adjSelectStart;
                data->SelectionEnd = adjSelectEnd;
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
        var decorations = GetDecorations(input);
        textDecorator.DecorateInputText(label, textCursorPosition, ref text, size, decorations);
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

    private IEnumerable<InputTextDecoration> GetDecorations(SeString input) {
        var textOffset = 0;
        foreach (var payload in input.Payloads) {
            if (payload is AutoTranslatePayload atPayload) {
                var startOffset = textOffset;
                var endOffset = textOffset + new StringInfo(atPayload.Text).LengthInTextElements;
                yield return new InputTextDecoration.TextColor(startOffset, startOffset + 1, ImGui.ColorConvertFloat4ToU32(Colors.AutoTranslateStartGreen));
                yield return new InputTextDecoration.TextColor(endOffset -1, endOffset, ImGui.ColorConvertFloat4ToU32(Colors.AutoTranslateEndRed));

                textOffset = endOffset;
            } else if (payload is ITextProvider textPayload) {
                var textInfo = new StringInfo(textPayload.Text);
                textOffset += textInfo.LengthInTextElements;
            }
        }
    }

    private void UpdateScrollX() {
        var size = ImGui.GetItemRectSize();
        var scrollIncrementX = size.X * 0.25f;
        var visibleWidth = size.X - ImGui.GetStyle().FramePadding.X;
    }

    private (int, int) ExpandSelectionAcrossAutoTranslate(SeString input, int startPos, int endPos) {
        // Note: Start/End are bi-directional and depend on the direction of selection when dragging
        var lower = startPos <= endPos ? startPos : endPos;
        var higher = startPos <= endPos ? endPos : startPos;

        var textOffset = 0;
        foreach (var payload in input.Payloads) {
            // We count UTF-8 bytes since ImGui's selection startPos and endPos seem to be straight char counts.
            // We can't just use `.Count` because C# strings are UTF-16.
            if (payload is ITextProvider textPayload) {
                var textLength = Encoding.UTF8.GetByteCount(textPayload.Text.ReplaceLineEndings("\n"));

                var payloadStart = textOffset;
                var payloadEnd = textOffset + textLength;

                if (payload is AutoTranslatePayload) {
                    var lowerInPayload = lower >= (payloadStart + 1) && lower < payloadEnd;
                    if (lowerInPayload) {
                        lower = payloadStart;
                    }

                    var higherInPayload = higher >= (payloadStart + 1) && higher < payloadEnd;
                    if (higherInPayload) {
                        higher = payloadEnd;
                    }
                }

                textOffset = payloadEnd;
            }
        }

        if (startPos <= endPos) { return (lower, higher); }
        else { return (higher, lower); }
    }
}
