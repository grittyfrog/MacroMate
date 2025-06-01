using System.Collections.Generic;
using System.Linq;
using Dalamud.Utility;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using Sprache;

namespace MacroMate.Extensions.Dalamud.AutoComplete;

public class ParsedCompletion {
    private Completion Underlying { get; set; }

    public uint RowId => Underlying.RowId;
    public uint Group => Underlying.Group;
    public uint Key => Underlying.Key;
    public ReadOnlySeString Text => Underlying.Text;
    public ReadOnlySeString GroupTitle => Underlying.GroupTitle;
    public required Lookup? LookupTable { get; init; }

    public class Lookup {
        public interface Entry {
            public record class RowRange(uint StartRow, uint EndRow) : Lookup.Entry;
            public record class Column(uint Col) : Lookup.Entry;
            public record class Noun() : Lookup.Entry;
        }

        public required List<Entry> Entries { get; init; }

        /// <summary>
        /// The name of another Excel sheet, indicates we should lookup names from this sheet instead of
        /// directly from Completion
        /// </summary>
        public required string TableName { get; init; }

        /// <summary>
        /// The column index that should be used to lookup names for this table.
        /// </summary>
        public required List<uint> TableNameColumns { get; init; }

        /// <summary>
        /// True if the `noun` specifier is included
        /// </summary>
        public required bool IsNoun { get; init; }
    }

    public static ParsedCompletion From(Completion completion) {
        return new ParsedCompletion {
            Underlying = completion,
            LookupTable = ParseLookup(completion)
        };
    }

    public static Lookup? ParseLookup(Completion completion) {
        if (completion.Key != 0) { return null; }

        var table = Env.SeStringEvaluator.Evaluate(completion.LookupTable).ExtractText();
        if (table == "@") { return null; }

        var (lookupTableName, lookupEntries) = ParseLookupTable.End().Parse(table);
        var columns = lookupEntries.OfType<Lookup.Entry.Column>().Select(c => c.Col).ToList();
        return new Lookup {
            Entries = lookupEntries.ToList(),
            TableName = lookupTableName,
            TableNameColumns = columns.Count > 0 ? columns : new List<uint> { 0 },
            IsNoun = lookupEntries.Any(e => e is Lookup.Entry.Noun)
        };
    }

    private static readonly Parser<Lookup.Entry> ParseLookupEntryRowRange =
        from startRow in Parse.Number
        from _ in Parse.Char('-')
        from endRow in Parse.Number
        select new Lookup.Entry.RowRange(uint.Parse(startRow), uint.Parse(endRow));

    private static readonly Parser<Lookup.Entry> ParseLookupEntryRowSingle =
        from row in Parse.Number
        select new Lookup.Entry.RowRange(uint.Parse(row), uint.Parse(row));

    private static readonly Parser<Lookup.Entry> ParseLookupEntryColumn =
        from _ in Parse.String("col-")
        from col in Parse.Number.Text()
        select new Lookup.Entry.Column(uint.Parse(col));

    private static readonly Parser<Lookup.Entry> ParseLookupEntryNoun =
        from _ in Parse.String("noun")
        select new Lookup.Entry.Noun();

    private static readonly Parser<Lookup.Entry> ParseLookupEntry =
        ParseLookupEntryNoun
            .Or(ParseLookupEntryColumn)
            .Or(ParseLookupEntryRowRange)
            .Or(ParseLookupEntryRowSingle);

    private static readonly Parser<IEnumerable<Lookup.Entry>> ParseLookupEntries =
        from _ in Parse.Char('[').Token()
        from entries in ParseLookupEntry.DelimitedBy(Parse.Char(',').Token())
        from _2 in Parse.Char(']').Token()
        select entries;

    private static readonly Parser<(string, IEnumerable<Lookup.Entry>)> ParseLookupTable =
        from lookupTableName in Parse.CharExcept('[').AtLeastOnce().Text()
        from lookupEntries in ParseLookupEntries.XOptional()
        select (lookupTableName, lookupEntries.GetOrDefault() ?? new List<Lookup.Entry>());
}
