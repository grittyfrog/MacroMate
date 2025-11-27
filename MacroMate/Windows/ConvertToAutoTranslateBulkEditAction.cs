using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using MacroMate.Extensions.Dalamud.AutoComplete;
using MacroMate.Extensions.Imgui;
using MacroMate.MacroTree;

namespace MacroMate.Windows;

internal class ConvertToAutoTranslateBulkEditAction : MacroBulkEdit {
    public bool Applied { get; set; } = false;
    private int minimumWordLength = 3;
    private int maximumWords = 3;
    private bool autoTranslateCommands = true;
    private int convertedWordsCount = 0;
    private readonly CompareInfo compareInfo = CultureInfo.InvariantCulture.CompareInfo;

    public void DrawLabel(int id) {
        ImGui.TextUnformatted("Convert to Auto-Translate");
        ImGuiExt.HoverTooltip("Converts plain text words to Auto-Translate payloads if they match the Auto-Translate dictionary");
    }

    public void DrawValue(int id) {
        ImGui.BeginGroup();

        ImGui.TextUnformatted("Min Word Length:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        ImGui.SliderInt("###min_word_length", ref minimumWordLength, 1, 10);
        ImGuiExt.HoverTooltip("Skip words shorter than this length to avoid replacing common short words");

        ImGui.TextUnformatted("Max Words:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        ImGui.SliderInt("###max_words", ref maximumWords, 1, 5);
        ImGuiExt.HoverTooltip("Maximum number of consecutive words to match (e.g., 3 allows \"Fell Cleave\" to match)");

        ImGui.Checkbox("Auto Translate Commands", ref autoTranslateCommands);
        ImGuiExt.HoverTooltip("Convert commands like /action to Auto-Translate payloads (commands are the same in all languages)");

        if (Applied && convertedWordsCount > 0) {
            ImGui.TextUnformatted($"Converted {convertedWordsCount} word(s)");
        }

        ImGui.EndGroup();
    }

    public void ApplyTo(MateNode.Macro target) {
        if (Env.CompletionIndex.State != CompletionIndex.IndexState.INDEXED) {
            return; // Can't convert if index isn't ready
        }

        var convertedWords = 0;
        var newSeString = ConvertTextToAutoTranslate(target.Lines, ref convertedWords);
        target.Lines = newSeString;
        convertedWordsCount += convertedWords;
    }

    private SeString ConvertTextToAutoTranslate(SeString input, ref int convertedCount) {
        var builder = new SeStringBuilder();

        foreach (var payload in input.Payloads) {
            if (payload is TextPayload textPayload && textPayload.Text != null) {
                // Process text payload to convert words to auto-translate
                var convertedPayloads = ConvertTextPayload(textPayload.Text, ref convertedCount);
                foreach (var p in convertedPayloads) {
                    builder.Add(p);
                }
            } else {
                // Keep other payloads (including existing AutoTranslatePayloads) unchanged
                builder.Add(payload);
            }
        }

        return builder.Build();
    }

    private List<Payload> ConvertTextPayload(string text, ref int convertedCount) {
        var payloads = new List<Payload>();

        // Extract all word tokens (including commands like /word)
        var regex = new Regex(@"/\w+|\b\w+\b");
        var matches = regex.Matches(text);

        if (matches.Count == 0) {
            // No words found, return text as-is
            if (text.Length > 0) {
                payloads.Add(new TextPayload(text));
            }
            return payloads;
        }

        var currentPos = 0;
        var i = 0;

        while (i < matches.Count) {
            // Add any text before this match
            if (currentPos < matches[i].Index) {
                payloads.Add(new TextPayload(text.Substring(currentPos, matches[i].Index - currentPos)));
            }

            // Try to match the longest sequence starting at position i
            CompletionInfo? longestMatch = null;
            int longestWordCount = 0;
            int longestEndPos = 0;
            string longestPhrase = "";

            for (var wordCount = Math.Min(maximumWords, matches.Count - i); wordCount >= 1; wordCount--) {
                var startPos = matches[i].Index;
                var endPos = matches[i + wordCount - 1].Index + matches[i + wordCount - 1].Length;
                var phrase = text.Substring(startPos, endPos - startPos);

                // Check if this is a command phrase - if commands are disabled, skip
                var isCommandPhrase = phrase.StartsWith("/");
                if (isCommandPhrase && !autoTranslateCommands) {
                    continue;
                }

                // Check minimum word length (using first word without slash)
                var firstWord = matches[i].Value;
                var firstWordWithoutSlash = firstWord.StartsWith("/") ? firstWord.Substring(1) : firstWord;
                if (firstWordWithoutSlash.Length < minimumWordLength) {
                    continue;
                }

                // Try to find exact match in CompletionIndex
                var completions = Env.CompletionIndex.Search(phrase)
                    .Where(c => compareInfo.Compare(c.SeString.ExtractText(), phrase, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0)
                    .ToList();

                if (completions.Count > 0) {
                    longestMatch = completions[0];
                    longestWordCount = wordCount;
                    longestEndPos = endPos;
                    longestPhrase = phrase;
                    break; // Found longest match, no need to try shorter ones
                }
            }

            if (longestMatch != null) {
                // Found a match, add as AutoTranslatePayload
                payloads.Add(new AutoTranslatePayload(longestMatch.Group, longestMatch.Key));
                convertedCount++;
                currentPos = longestEndPos;
                i += longestWordCount;
            } else {
                // No match, add the current word as text
                payloads.Add(new TextPayload(matches[i].Value));
                currentPos = matches[i].Index + matches[i].Length;
                i++;
            }
        }

        // Add any remaining text after the last match
        if (currentPos < text.Length) {
            payloads.Add(new TextPayload(text.Substring(currentPos)));
        }

        return payloads;
    }

    public void Dispose() {}

    public MacroBulkEdit.Factory FactoryRef => Factory;
    public static MacroBulkEdit.Factory Factory => new BulkEditFactory();
    internal class BulkEditFactory : MacroBulkEdit.Factory {
        public string Name => "Convert to Auto-Translate";
        public MacroBulkEdit Create() => new ConvertToAutoTranslateBulkEditAction();
    }
}
