using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.Sheets;
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

    ReadOnlySeString SeString,

    /// <summary>
    /// Some completions include help text (i.e. TextCommand). If this is non-empty then it may be
    /// displayed to the user.
    /// </summary>
    ReadOnlySeString? HelpText
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
                            SeString: text,
                            HelpText: GetHelpText(completion.LookupTable.TableName, rowId)
                        );
                    }
                }
            }
        } else {
            yield return new CompletionInfo(
                Group: completion.Group,
                GroupTitle: null, // Can't use our GroupTitle, it's just '-'
                Key: completion.RowId,
                SeString: completion.Text,
                HelpText: null
            );
        }
    }

    private static ReadOnlySeString? GetHelpText(string table, uint rowId) {
        if (TryGetRowByName<GuardianDeity>(table, rowId, out var gd)) { return gd.Description; }
        if (TryGetRowByName<GeneralAction>(table, rowId, out var ga)) { return ga.Description; }
        if (TryGetTransientRowByName<ActionTransient>(table, rowId, out var action)) { return action.Description; }
        if (TryGetRowByName<CraftAction>(table, rowId, out var craftAction)) { return craftAction.Description; }
        if (TryGetRowByName<BuddyAction>(table, rowId, out var buddyAction)) { return buddyAction.Description; }
        if (TryGetRowByName<PetAction>(table, rowId, out var petAction)) { return petAction.Description; }
        if (TryGetRowByName<MainCommand>(table, rowId, out var mainCommand)) { return mainCommand.Description; }
        if (TryGetRowByName<TextCommand>(table, rowId, out var textCommand)) { return textCommand.Description; }
        if (TryGetTransientRowByName<CompanionTransient>(table, rowId, out var companion)) { return companion.Tooltip; }
        if (TryGetRowByName<MKDSupportJob>(table, rowId, out var occJob)) { return occJob.Unknown3; }

        return null;
    }

    private static bool TryGetRowByName<T>(
        string tableName,
        uint rowId,
        [MaybeNullWhen(false)] out T row
    ) where T : struct, IExcelRow<T> {
        if (tableName == typeof(T).Name) {
            if (Env.DataManager.GetExcelSheet<T>().TryGetRow(rowId, out var rowInner)) {
                row = rowInner;
                return true;
            }
        }
        row = default(T);
        return false;
    }

    private static bool TryGetTransientRowByName<T>(
        string tableName,
        uint rowId,
        [MaybeNullWhen(false)] out T row
    ) where T : struct, IExcelRow<T> {
        if ($"{tableName}Transient" == typeof(T).Name) {
            if (Env.DataManager.GetExcelSheet<T>().TryGetRow(rowId, out var rowInner)) {
                row = rowInner;
                return true;
            }
        }
        row = default(T);
        return false;
    }
}
