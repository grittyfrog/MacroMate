using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace MacroMate.Extensions.Dalamud.Str;

public static partial class SeStringEx {
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

    public static int CountNewlines(this SeString self) =>
        self.Payloads.Count(p => p.Type == PayloadType.NewLine);

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
        return Enumerable.Max(self.SplitIntoLines().Select(s => s.Length()));
    }

    public static bool IsNotEmpty(this SeString self) {
        return self.Payloads.Count > 0;
    }

    public static IEnumerable<SeString> SplitIntoLines(this SeString self) {
        var chunk = new SeString();
        foreach (var payload in self.Payloads) {
            if (payload.Type == PayloadType.NewLine) {
                if (chunk.Payloads.Count > 0) {
                    yield return chunk;
                    chunk = new SeString();
                }
            } else if (payload is TextPayload textPayload && textPayload.Text != null) {
                var splits = textPayload.Text.Split(new string[] { "\r\n", "\n", "\r" }, System.StringSplitOptions.None);
                chunk.Append(splits[0]);
                foreach (var split in splits.Skip(1)) {
                    yield return chunk;
                    chunk = new SeString();
                    chunk.Append(split);
                }
            } else {
                chunk.Payloads.Add(payload);
            }
        }

        if (chunk.Payloads.Count > 0) {
            yield return chunk;
        }
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
}
