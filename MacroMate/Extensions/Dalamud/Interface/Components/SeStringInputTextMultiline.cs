using System;
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
    private StbTextEditState? previousCursorState = null;

    private unsafe ImGuiInputTextState* TextState =>
        (ImGuiInputTextState*)(ImGui.GetCurrentContext() + ImGuiInputTextState.TextStateOffset);

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

        var ctrlVPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Ctrl) && ImGui.IsKeyPressed(ImGuiKey.V, false);
        var shiftInsPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Shift) && ImGui.IsKeyPressed(ImGuiKey.Insert, false);
        var isPasting = ctrlVPressed || shiftInsPressed;

        var ctrlXPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Ctrl) && ImGui.IsKeyPressed(ImGuiKey.X, false);
        var shiftDelPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Shift) && ImGui.IsKeyPressed(ImGuiKey.Delete, false);
        var isCut = ctrlXPressed || shiftDelPressed;

        var ctrlCPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Ctrl) && ImGui.IsKeyPressed(ImGuiKey.C, false);
        var ctrlInsertPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Ctrl) && ImGui.IsKeyPressed(ImGuiKey.Insert, false);
        var isCopy = ctrlCPressed || ctrlInsertPressed;

        ImGuiInputTextCallback decoratedCallback;
        unsafe {
            decoratedCallback = (data) => {
                var result = callback(data);

                // We supress all ImGui pasting so we can manually do it ourself later in this function.
                // This will be called once for every charcter that is pasted, and will supress them all.
                if (isPasting) {
                    data->EventChar = 0;
                    processingPasteEvent = true;
                }

                // ImGui "Start" and "End" can be in either direction depending on the direction of selection,
                // so we normalize it to make the cut/copy/paste code easier
                var lower = data->SelectionStart <= data->SelectionEnd ? data->SelectionStart : data->SelectionEnd;
                var higher = data->SelectionStart <= data->SelectionEnd ? data->SelectionEnd : data->SelectionStart;
                var selectionLength = higher - lower;

                // We've finished supressing the paste events
                if (processingPasteEvent && !isPasting) {
                    var clipboardUtf8 = ClipboardEx.GetPayloadEnabledClipboardString();
                    if (clipboardUtf8 != null) {
                        var payloadEnabledClipboardText = SeString.Parse(clipboardUtf8.Value);

                        IndexPayloads(payloadEnabledClipboardText);

                        var ptr = new ImGuiInputTextCallbackDataPtr(data);

                        if (selectionLength > 0) {
                            ptr.DeleteChars(lower, selectionLength);
                        }
                        ptr.InsertChars(data->CursorPos, payloadEnabledClipboardText.TextValue.ReplaceLineEndings("\n"));
                        processingPasteEvent = false;
                    }
                }

                // If we are cutting or copying we should let ImGui handle the text manipulation, but we should
                // also copy the relevant SeString parts to the in-game clipboard so they can be pasted to
                // SeStringInputTextMultiline or in-game windows.
                if (isCut || isCopy) {
                    var bytes = new Span<byte>(data->Buf, data->BufTextLen);
                    var selectedBytes = bytes.Slice(lower, selectionLength);
                    var selectedText = Encoding.UTF8.GetString(selectedBytes);
                    var selectedSeString = SeStringEx
                        .ParseFromText(selectedText, knownTranslationPayloads)
                        .NormalizeNewlines()
                        .ReplaceNewlinesWithCR();
                    ClipboardEx.SetClipboardSeString(selectedSeString);
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

        var decorations = GetDecorations(input);
        textDecorator.DecorateInputText(label, ref text, size, decorations);

        // We apply cursor/selection adjustment after parsing `input` in case it has changed.
        ApplyAutoTranslateCursorAndSelectionBehaviour(input);

        return edited;
    }

    public int? GetCursorPos() {
        // Internal cursor is UTF-16 so it matches C# representation
        unsafe { return TextState->Stb.Cursor; }
    }

    /// Gets the SeString represented by the selection, or null if no selection
    public SeString? SelectedSeString() {
        string selectedText;
        unsafe {
            selectedText = TextState->SelectedText();
        }

        if (selectedText != "") {
            var selectedSeString = SeStringEx
                .ParseFromText(selectedText, knownTranslationPayloads)
                .NormalizeNewlines()
                .ReplaceNewlinesWithCR();
            return selectedSeString;
        }

        return null;
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
                yield return new InputTextDecoration.TextColor(endOffset - 1, endOffset, ImGui.ColorConvertFloat4ToU32(Colors.AutoTranslateEndRed));

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

    /// Mimic the vanilla behaviour of the selection/cursor.
    /// Returns the new cursor position, selection start and selection end respectively
    private unsafe void ApplyAutoTranslateCursorAndSelectionBehaviour(SeString input) {
        var textState = TextState;

        // These are counted in wchar-positions, rather then UTF8 positions
        var (startPos, endPos, cursor) = textState->SelectionTuple;

        // If our previous selection hasn't changed then we don't need to re-run this logic.
        //
        // This also seems to prevent the "backspacing near auto-translate causes it to auto-select" bug, though
        // I didn't dig deep enough to be sure of why
        //
        // If we've release the mouse button we also want to keep going, since we might have released in the middle
        // of an auto-translate and now want to widen our selection (which we supressed earlier to avoid a "bouncy" selection)
        var cursorOrSelectionChanged = previousCursorState == null || previousCursorState.Value.Cursor != cursor && previousCursorState.Value.SelectStart != startPos && previousCursorState.Value.SelectEnd != endPos;
        if (!cursorOrSelectionChanged && !ImGui.IsMouseReleased(ImGuiMouseButton.Left)) {
            return;
        }

        var lower = startPos <= endPos ? startPos : endPos;
        var higher = startPos <= endPos ? endPos : startPos;

        var textOffset = 0;
        foreach (var payload in input.Payloads) {
            // We can count UTF-16 bytes here since ImGui uses wchar for the internal InputText state which
            // matches C#'s encoding.
            //
            // If we were using the data from the callback event we'd need to count UTF-8, since that's what
            // it (and most of ImGui) uses
            if (payload is ITextProvider textPayload) {
                var textLength = textPayload.Text.ReplaceLineEndings("\n").Length;

                var payloadStart = textOffset;
                var payloadEnd = textOffset + textLength;

                if (payload is AutoTranslatePayload) {
                    var lowerInPayload = lower >= (payloadStart + 2) && lower < payloadEnd;
                    if (lowerInPayload) {
                        lower = payloadStart;
                    }

                    var higherInPayload = higher >= (payloadStart + 2) && higher < payloadEnd;
                    if (higherInPayload) {
                        higher = payloadEnd;
                    }
                }

                textOffset = payloadEnd;
            }
        }

        previousCursorState = textState->Stb;

        if (startPos <= endPos) {
            textState->Stb.SelectStart = lower;
            textState->Stb.SelectEnd = higher;
            textState->Stb.Cursor = higher;
        } else {
            textState->Stb.SelectStart = higher;
            textState->Stb.SelectEnd = lower;
            textState->Stb.Cursor = lower;
        }
    }
}
