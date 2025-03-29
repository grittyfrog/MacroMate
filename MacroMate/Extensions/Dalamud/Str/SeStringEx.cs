using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Extensions.Dalamud.Str;

public static partial class SeStringEx {
    public static bool IsSame(this SeString self, SeString other) {
        var selfStr = self.ToString().ReplaceLineEndings();
        var otherStr = other.ToString().ReplaceLineEndings();
        return selfStr.Equals(otherStr);
    }

    /// <summary>Normalize all newline characters to actual NewLinePayloads</summary>
    public static SeString NormalizeNewlines(this SeString self) {
        return SeStringEx.JoinFromLines(self.SplitIntoLines());
    }

    public static SeString ReplaceNewlinesWithCR(this SeString self) {
        return new SeString(self.Payloads.Select(payload => {
            if (payload is NewLinePayload) {
                return new TextPayload("\r");
            } else {
                return payload;
            }
        }).ToList());
    }

    /// <summary>Get the number of characters used by this string</summary>
    public static int Length(this SeString self) {
        // TODO: Some payloads don't actually use the number of rendered characters
        // (i.e. Auto Translate uses 3 chars, not it's expanded value)
        //
        // Ideally we should account for this.
        return self.TextValue.Count();
    }

    /// <summary>Get the length of the longest line in this string</summary>
    public static int MaxLineLength(this SeString self) {
        if (self.Payloads.Count == 0) { return 0; }
        return Enumerable.Max(self.SplitIntoLines().Select(s => s.Length()).DefaultIfEmpty());
    }

    public static bool IsNotEmpty(this SeString self) {
        return self.Payloads.Count > 0;
    }

    public static IEnumerable<SeString> SplitIntoLines(this SeString self) {
        var chunk = new SeString();
        foreach (var payload in self.Payloads) {
            if (payload.Type == PayloadType.NewLine) {
                if (chunk.Payloads.Count > 0) {
                    yield return chunk.CompressText();
                    chunk = new SeString();
                }
            } else if (payload is TextPayload textPayload && textPayload.Text != null) {
                var splits = textPayload.Text.Split(new string[] { "\r\n", "\n", "\r" }, System.StringSplitOptions.None);
                chunk.Append(splits[0]);

                foreach (var (split, index) in splits.WithIndex().Skip(1)) {
                    yield return chunk.CompressText();
                    chunk = new SeString();
                    chunk.Append(split);
                }
            } else {
                chunk.Payloads.Add(payload);
            }
        }

        if (chunk.Payloads.Count > 0 && chunk.TextValue != "") {
            yield return chunk.CompressText();
        }
    }

    /// <summary>
    /// Joins adjacent text payloads
    /// </summary>
    public static SeString CompressText(this SeString self) {
        var compressed = new SeString();

        foreach (var payload in self.Payloads) {
            if (compressed.Payloads.Count == 0) {
                compressed.Append(payload);
                continue;
            }

            var currentPayload = compressed.Payloads.Last();
            if (currentPayload is TextPayload ctp && payload is TextPayload ntp) {
                ctp.Text = ctp.Text + ntp.Text;
                continue;
            }

            compressed.Append(payload);
        }

        return compressed;
    }

    /// <summary>
    /// Inverse of [SplitIntoLines]. Rejoins all SeStrings in [lines] into a single SeString, separated by lines.
    /// </summary>
    public static SeString JoinFromLines(IEnumerable<SeString> lines) {
        var builder = new SeStringBuilder();

        var enumerator = lines.GetEnumerator();

        var hasValue = enumerator.MoveNext();
        if (hasValue) {
            builder.Append(enumerator.Current);
        } else {
            return builder.Build();
        }

        while (enumerator.MoveNext()) {
            builder.Add(new NewLinePayload());
            builder.Append(enumerator.Current);
        }

        return builder.Build();
    }

    /// <summary>
    /// Insert [text] into [self] at [offset].
    ///
    /// Offset is calculated based on the TextValue rendering of this SeString
    /// </summary>
    public static SeString InsertAtTextValueIndex(this SeString self, SeString text, int offset) {
        var updatedString = new SeStringBuilder();
        var textOffset = 0;

        // If we are empty just insert and leave
        if (self.Payloads.Count == 0) {
            text.Payloads.ForEach(pl => { updatedString.Add(pl); });
            return updatedString.Build();
        }

        var remainingPayloads = new Queue<Payload>(self.Payloads);
        while (remainingPayloads.TryDequeue(out var payload)) {
            var textPayload = payload as ITextProvider;
            if (textPayload == null || textPayload.Text == null) {
                updatedString.Add(payload);
                continue;
            }

            var startOffset = textOffset;
            var endOffset = textOffset + new StringInfo(textPayload.Text).LengthInTextElements;
            textOffset = endOffset;

            // If our offset is within the range then we want to insert
            if (startOffset <= offset && offset <= endOffset) {
                // If we are a text payload we can splice the text, otherwise just put it at the front.
                if (textPayload is TextPayload tp && tp.Text != null) {
                    var textPayloadInsertOffset = offset - startOffset;
                    if (text.Payloads.Count == 1 && text.Payloads[0] is TextPayload newTextPayload) {
                        var newText = tp.Text.Insert(textPayloadInsertOffset, newTextPayload.Text ?? "");
                        updatedString.AddText(newText);
                    } else {
                        var beforeText = textPayload.Text[0..textPayloadInsertOffset];
                        var afterText = textPayload.Text[textPayloadInsertOffset..];
                        if (beforeText.Count() > 0) {
                            updatedString.AddText(beforeText);
                        }
                        text.Payloads.ForEach(pl => { updatedString.Add(pl); });
                        if (afterText.Count() > 0) {
                            updatedString.AddText(afterText);
                        }
                    }
                } else {
                    // If we can't split the payload just put it at the end
                    updatedString.Add(payload);
                    text.Payloads.ForEach(pl => { updatedString.Add(pl); });
                }
                break;
            } else {
                updatedString.Add(payload);
            }
        }

        // Any remaining payloads can just be appended
        while (remainingPayloads.TryDequeue(out var payload)) {
            updatedString.Add(payload);
        }

        return updatedString.Build();
    }
}
