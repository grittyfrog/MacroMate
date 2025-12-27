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

    public static Lookup? ParseLookup(Completion completion)
    {
        // 1. 原有逻辑：Key != 0 的直接返回null
        if (completion.Key != 0)
        {
            return null;
        }

        // 2. 获取LookupTable的字符串表示
        var table = Env.SeStringEvaluator.Evaluate(completion.LookupTable).ExtractText();

        // 3. 核心修复：过滤掉 API 14 中无效的数据格式
        // 如果值是 空字符串、空白 或 单独的"@"符号，直接返回 null，表示跳过此条数据。
        if (string.IsNullOrWhiteSpace(table) || table == "@")
        {
            return null;
        }

        // 4. 此时，table 应该是类似 "Mount[1-1,4-6,...]" 的有效格式
        // 尝试用原有的解析器进行解析
        try
        {
            var (lookupTableName, lookupEntries) = ParseLookupTable.End().Parse(table);
            var columns = lookupEntries.OfType<Lookup.Entry.Column>().Select(c => c.Col).ToList();
            return new Lookup
            {
                Entries = lookupEntries.ToList(),
                TableName = lookupTableName,
                TableNameColumns = columns.Count > 0 ? columns : new List<uint> { 0 },
                IsNoun = lookupEntries.Any(e => e is Lookup.Entry.Noun)
            };
        }
        catch (ParseException)
        {
            // 如果解析失败（理论上不应该发生，因为格式已经过滤），也返回null
            // Env.PluginLog.Debug($"Failed to parse valid-looking table: {table}");
            return null;
        }
        // 注意：不再捕获所有异常，让其他意外错误暴露
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
