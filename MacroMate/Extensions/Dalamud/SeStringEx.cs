using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

namespace MacroMate.Extensions.Dalamud;

public static class SeStringEx {
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
                var splits = textPayload.Text.Split(
                    new char[] { '\n', '\r' },
                    System.StringSplitOptions.RemoveEmptyEntries
                );
                foreach (var split in splits) {
                    chunk.Append(split);
                    yield return chunk;
                    chunk = new SeString();
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


    /// <summary>
    /// Encodes SeString payloads that are also ITextProviders into a C# UTF-16 string.
    /// </summary>
    // ///
    // /// <example>
    // /// For example:
    // /// <code>
    // /// var seString = new SeStringBuilder()
    // ///   .Append("Hello ")
    // ///   .Append(new AutoTranslatePayload(0, 0))
    // /// </code>
    // /// Produces "Hello {0,0}"
    // /// </example>
    // public static string EncodeToMacroString(this SeString seString) {
    //     return seString.Payloads
    //         .SelectNonNull(payload => {
    //             if (payload.Type == PayloadType.AutoTranslateText) {
    //                 // TODO: Try to get these made public in Dalamud so we can stop using reflection here
    //                 var group = payload.GetFieldValue<uint>("group");
    //                 var key = payload.GetFieldValue<uint>("key");
    //                 return $"{(char)SeIconChar.AutoTranslateOpen}{group},{key}{(char)SeIconChar.AutoTranslateClose}";
    //             } else if (payload is ITextProvider textPayload){
    //                 return textPayload.Text;
    //             } else {
    //                 return null;
    //             }
    //         })
    //         .Let(texts => string.Join("", texts));
    // }

    // /// <summary>
    // /// Construct a SeString from a string build using [SeString.TextValue]. This can reconstruct payloads that
    // /// implement [ITextProvider] (such as [AutoTranslatePayload])
    // /// </summary>
    // public static SeString DecodeFromMacroString(string s) {
    // }
}
