using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using MacroMate.Extensions.Dotnet;
using Sprache;

namespace MacroMate.Extensions.Dalamud.Str;

public static partial class SeStringEx {
    /// Parse a SeString from a string, we assume the string is the result of calling `SeString.TextValue` and
    /// attempt to reconstruct the recognisable payloads (currently just Auto Translate).
    ///
    /// This function also recognises MacroMate generated auto-translate payloads of the form {My translation|0,100} (where {} are auto translate start/end).
    /// These are usually used by the plugin config files.
    ///
    /// Only Auto-Translate payloads that are included in [knownTranslationPayloads] will be recognised.
    public static SeString ParseFromText(
        string text,
        Dictionary<string, AutoTranslatePayload>? knownTranslationPayloads = null
    ) {
        var chunks = SeStringChunk.Of(text);
        var payloads = chunks.Select(chunk => chunk.ToPayload(knownTranslationPayloads));
        return new SeString(payloads.ToList());
    }

    public static string Unparse(this SeString self) {
        var chunks = self.Payloads
            .Select(payload => SeStringChunk.FromPayload(payload))
            .WithoutNull();
        return chunks.Select(chunk => chunk.Unparse()).Let(chunkTexts => string.Join("", chunkTexts));
    }

    internal interface SeStringChunk {
        private static readonly char AutoTranslateStart = '\uE040';
        private static readonly char AutoTranslateEnd = '\uE041';

        public string Unparse();

        public record class Text(string text) : SeStringChunk {
            public string Unparse() => text;
        }
        public record class AutoTranslateText(
            string activeLanguageText,
            AutoTranslateIds? ids = null
        ) : SeStringChunk {
            public string Unparse() {
                var idPart = ids?.Let(id => $"|{id.group},{id.key}") ?? "";
                return $"{AutoTranslateStart}{activeLanguageText}{idPart}{AutoTranslateEnd}";
            }
        }
        public record class AutoTranslateIds(uint group, uint key) {}

        private static readonly Parser<AutoTranslateIds> autoTranslateIdParser =
            from _ in Parse.Char('|')
            from grp in Parse.Number.Select(n => UInt32.Parse(n))
            from _2 in Parse.Char(',')
            from key in Parse.Number.Select(n => UInt32.Parse(n))
            select new AutoTranslateIds(grp, key);

        private static readonly Parser<SeStringChunk> autoTranslatePayload =
            from _ in Parse.Char(AutoTranslateStart)
            from text in Parse.CharExcept(c => c == '|' || c == AutoTranslateEnd, "").Many().Text()
            from ids in Parse.Optional(autoTranslateIdParser)
            from _2 in Parse.Char(AutoTranslateEnd)
            select new AutoTranslateText(text.Trim(), ids.GetOrDefault());

        private static readonly Parser<SeStringChunk> textParser =
            Parse.CharExcept(c => c == AutoTranslateStart, "").Many()
                .Text()
                .Select(s => new Text(s));

        private static readonly Parser<SeStringChunk> seStringChunkParser =
            autoTranslatePayload.Or(textParser);

        public static IEnumerable<SeStringChunk> Of(string text) =>
            seStringChunkParser.Many().Parse(text);

        public Payload ToPayload(Dictionary<string, AutoTranslatePayload>? knownTranslationPayloads = null) {
            switch (this) {
                case SeStringChunk.Text textChunk: return new TextPayload(textChunk.text);
                case SeStringChunk.AutoTranslateText atChunk:
                    if (atChunk.ids != null) {
                        return new AutoTranslatePayload(atChunk.ids.group, atChunk.ids.key);
                    } else if (knownTranslationPayloads != null && knownTranslationPayloads.TryGetValue(atChunk.activeLanguageText, out var atPayload)) {
                        return atPayload;
                    } else {
                        return new TextPayload(atChunk.activeLanguageText);
                    }
                default: throw new Exception("Unrecognised chunk type");
            }
        }

        public static SeStringChunk? FromPayload(Payload payload) {
            switch (payload) {
                case AutoTranslatePayload atPayload:
                    var ids = new SeStringChunk.AutoTranslateIds(atPayload.Group, atPayload.Key);
                    return new SeStringChunk.AutoTranslateText(atPayload.RawText(), ids);
                case ITextProvider itp:
                    return new SeStringChunk.Text(itp.Text);
                default: return null;
            }
        }
    }
}

