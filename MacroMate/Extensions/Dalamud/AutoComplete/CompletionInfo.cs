using System.Collections.Generic;
using System.Linq;
using Lumina.Excel;
using Lumina.Text.ReadOnly;

namespace MacroMate.Extensions.Dalamud.AutoComplete;

/// <summary>
/// Represents an auto-completeable text string, typically derived from the Completion table
/// </summary>
public record class CompletionInfo(
    /// <summary>The group in `Completions` this info belongs to</summary>
    uint Group,

    ReadOnlySeString? GroupTitle,

    /// <summary>The row in `table` whose name is the actual auto-complete value</summary>
    uint Key,

    ReadOnlySeString SeString
) {
    public string GroupTitleString => (GroupTitle?.ExtractText() ?? "").Trim(' ', '.');

    public static IEnumerable<CompletionInfo> From(ParsedCompletion completion) {
        if (completion.LookupTable != null) {
            var table = Env.DataManager.GetExcelSheet<RawRow>(name: completion.LookupTable.TableName);

            var rowRanges = completion.LookupTable.Entries.OfType<ParsedCompletion.Lookup.Entry.RowRange>().ToList();
            if (rowRanges.Count == 0) {
                // If we have no ranges, we want the entire table
                rowRanges.Add(new ParsedCompletion.Lookup.Entry.RowRange(
                    StartRow: 0,
                    EndRow: (uint)(table.Count - 1)
                ));
            }

            foreach (var rowRange in rowRanges) {
                for (var rowId = rowRange.StartRow; rowId <= rowRange.EndRow; ++rowId) {
                    var row = table.GetRowOrDefault(rowId);
                    if (row == null) { continue; }

                    // Some columns have repeated data, for example  `/fashionguide` is in both the `name` and `alias`
                    // columns.
                    //
                    // We want to de-dup them since they produce identical CompletionInfo.
                    var visited = new HashSet<ReadOnlySeString>();
                    foreach (var col in completion.LookupTable.TableNameColumns) {
                        var text = row.Value.ReadStringColumn((int)col);
                        if (text == "") { continue; }
                        if (visited.Contains(text)) { continue; }
                        visited.Add(text);
                        yield return new CompletionInfo(
                            Group: completion.Group,
                            GroupTitle: null, // Can't use our GroupTitle, it's just '-'
                            Key: row.Value.RowId,
                            SeString: text
                        );
                    }
                }
            }
        } else {
            yield return new CompletionInfo(
                Group: completion.Group,
                GroupTitle: null, // Can't use our GroupTitle, it's just '-'
                Key: completion.RowId,
                SeString: completion.Text
            );
        }
    }
}
