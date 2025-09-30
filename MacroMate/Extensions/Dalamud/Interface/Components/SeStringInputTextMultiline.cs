using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dalamud.Str;
using MacroMate.Extensions.Imgui;
using static MacroMate.Extensions.Dalamud.Str.SeStringEx;

namespace MacroMate.Extensions.Dalamaud.Interface.Components;

public class SeStringInputTextMultiline {
    private bool processingPasteEvent = false;
    private Dictionary<string, AutoTranslatePayload> knownTranslationPayloads = new();
    private InputTextDecorator textDecorator = new();
    private STBTexteditState? previousCursorState = null;
    private (int, int)? previousEditWord = null; // The word range we want to edit if we get an auto-complete event

    private SeStringAutoCompletePopup AutoCompletePopup = new();

    private unsafe ImGuiInputTextStatePtr TextState => new(&ImGui.GetCurrentContext().Handle->InputTextState);

    public bool Draw(
        string label,
        ref SeString input,
        int maxLength,
        Vector2 size,
        ImGuiInputTextFlags flags,
        ImGui.ImGuiInputTextCallbackDelegate callback
    ) {
        AutoCompletePopup.Draw();

        IndexPayloads(input);

        bool edited = false;

        flags |= ImGuiInputTextFlags.CallbackAlways;
        flags |= ImGuiInputTextFlags.CallbackEdit;
        flags |= ImGuiInputTextFlags.CallbackCompletion;

        var ctrlVPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Ctrl) && ImGui.IsKeyPressed(ImGuiKey.V, false);
        var shiftInsPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Shift) && ImGui.IsKeyPressed(ImGuiKey.Insert, false);
        var isPasting = ctrlVPressed || shiftInsPressed;

        var ctrlXPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Ctrl) && ImGui.IsKeyPressed(ImGuiKey.X, false);
        var shiftDelPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Shift) && ImGui.IsKeyPressed(ImGuiKey.Delete, false);
        var isCut = ctrlXPressed || shiftDelPressed;

        var ctrlCPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Ctrl) && ImGui.IsKeyPressed(ImGuiKey.C, false);
        var ctrlInsertPressed = (ImGui.GetIO().KeyMods == ImGuiModFlags.Ctrl) && ImGui.IsKeyPressed(ImGuiKey.Insert, false);
        var isCopy = ctrlCPressed || ctrlInsertPressed;

        var text = input.TextValue.ReplaceLineEndings("\n");

        ImGui.ImGuiInputTextCallbackDelegate decoratedCallback = (scoped ref ImGuiInputTextCallbackData data) => {
            var result = callback(ref data);
            
            // We supress all ImGui pasting so we can manually do it ourself later in this function.
            // This will be called once for every charcter that is pasted, and will supress them all.
            if (isPasting) {
                data.EventChar = 0;
                processingPasteEvent = true;
            }

            // ImGui "Start" and "End" can be in either direction depending on the direction of selection,
            // so we normalize it to make the cut/copy/paste code easier
            var lower = data.SelectionStart <= data.SelectionEnd ? data.SelectionStart : data.SelectionEnd;
            var higher = data.SelectionStart <= data.SelectionEnd ? data.SelectionEnd : data.SelectionStart;
            var selectionLength = higher - lower;

            // We've finished supressing the paste events
            if (processingPasteEvent && !isPasting) {
                var clipboardUtf8 = ClipboardEx.GetPayloadEnabledClipboardString();
                if (clipboardUtf8 != null) {
                    var payloadEnabledClipboardText = SeString.Parse(clipboardUtf8.Value);

                    IndexPayloads(payloadEnabledClipboardText);

                    if (selectionLength > 0) {
                        data.DeleteChars(lower, selectionLength);
                    }
                    data.InsertChars(data.CursorPos, payloadEnabledClipboardText.TextValue.ReplaceLineEndings("\n"));
                    processingPasteEvent = false;
                }
            }

            // If we are cutting or copying we should let ImGui handle the text manipulation, but we should
            // also copy the relevant SeString parts to the in-game clipboard so they can be pasted to
            // SeStringInputTextMultiline or in-game windows.
            if (isCut || isCopy)
            {
                var bytes = data.BufSpan;
                var selectedBytes = bytes.Slice(lower, selectionLength);
                var selectedText = Encoding.UTF8.GetString(selectedBytes);
                var selectedSeString = SeStringEx
                                       .ParseFromText(selectedText, knownTranslationPayloads)
                                       .NormalizeNewlines()
                                       .ReplaceNewlinesWithCR();
                ClipboardEx.SetClipboardSeString(selectedSeString);
            }

            if (data.EventFlag == ImGuiInputTextFlags.CallbackCompletion) {
                var currentWord = TextState.CurrentEditWord();
                if (currentWord != null) { 
                    previousEditWord = TextState.CurrentEditWordBounds();
                    AutoCompletePopup.AutoCompleteFilter = currentWord;
                    AutoCompletePopup.PopupPos =
                        ImGui.GetItemRectMin() +
                        ImGuiExt.InputTextCalcText2dPos(text, TextState.CurrentEditWordStart()) +
                        new Vector2(0, ImGui.GetFontSize() + ImGui.GetStyle().ItemSpacing.Y * 2);

                    AutoCompletePopup.Open();
                } 
            }

            return result;
        };

        // Reserve space for line numbers first
        var lineNumbers = InputTextMultilineLineNumbers.Reserve(text, size);

        // Draw the text input with reduced width
        var result = ImGui.InputTextMultiline(
            label,
            ref text,
            maxLength,
            lineNumbers.RemainingTextSize,
            flags,
            decoratedCallback
        );

        // Now draw the line numbers after the InputText is rendered
        lineNumbers.Draw(label);

        if (result) {
            input = SeStringEx.ParseFromText(text, knownTranslationPayloads);
            edited = true;
        }

        var focused = ImGui.IsItemFocused();

        while (AutoCompletePopup.CompletionEvents.TryDequeue(out var completion)) {
            if (!previousEditWord.HasValue) { break; }
            var (editStart, editEnd) = previousEditWord.Value;
            text = text.Remove(editStart, editEnd - editStart); // Remove current word

            var atChunk = new SeStringChunk.AutoTranslateText(
                "",
                new SeStringChunk.AutoTranslateIds(completion.Group, completion.Key)
            );
            text = text.Insert(editStart, atChunk.Unparse());

            input = SeStringEx.ParseFromText(text, knownTranslationPayloads);
            IndexPayloads(input);
            edited = true;
        }

        var decorations = GetDecorations(input);
        textDecorator.DecorateInputText(label, text, size, decorations);

        // We apply cursor/selection adjustment after parsing `input` in case it has changed.
        if (focused) {
            ApplyAutoTranslateCursorAndSelectionBehaviour(input);
        }

        return edited;
    }

    public int? GetCursorPos() {
        // Internal cursor is UTF-16 so it matches C# representation
        return TextState.Stb.Cursor; 
    }

    public (int, int) GetCurrentEditWordBounds() {
        // Internal cursor is UTF-16 so it matches C# representation
        return TextState.CurrentEditWordBounds(); 
    }

    /// Gets the SeString represented by the selection, or null if no selection
    public SeString? SelectedSeString() {
        var selectedText = TextState.SelectedText();

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
                yield return new InputTextDecoration.HoverTooltip(startOffset, endOffset, new(() => {
                    var completion = Env.CompletionIndex.ById(atPayload.Group, atPayload.Key);
                    return completion?.HelpText;
                }));

                textOffset = endOffset;
            } else if (payload is ITextProvider textPayload) {
                var textInfo = new StringInfo(textPayload.Text);
                textOffset += textInfo.LengthInTextElements;
            }
        }
    }

    /// Mimic the vanilla behaviour of the selection/cursor.
    /// Returns the new cursor position, selection start and selection end respectively
    private unsafe void ApplyAutoTranslateCursorAndSelectionBehaviour(SeString input) {
        var textState = TextState;

        // These are counted in wchar-positions, rather then UTF8 positions
        var startPos = textState.Stb.SelectStart;
        var endPos = textState.Stb.SelectEnd;
        var cursor = textState.Stb.Cursor;

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

        previousCursorState = textState.Stb;

        if (startPos <= endPos) {
            textState.Stb.SelectStart = lower;
            textState.Stb.SelectEnd = higher;
            textState.Stb.Cursor = higher;
        } else {
            textState.Stb.SelectStart = higher;
            textState.Stb.SelectEnd = lower;
            textState.Stb.Cursor = lower;
        }
    }
}
