using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KTrie;
using Lumina.Excel.Sheets;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Extensions.Dalamud.AutoComplete;

/// <summary>
/// Index to help look up completion information (i.e. Auto Translate)
/// </summary>
/// <remarks>
/// Heavily inspired by Chat2: https://github.com/Infiziert90/ChatTwo/blob/main/ChatTwo/Util/AutoTranslate.cs#L70
/// </remarks>
public class CompletionIndex {
    /// <summary>
    /// The `GroupTitle` field of most completion entries is empty, if we want to know the group title
    /// we need to look at the "special" group header completion, there should be 1 per GroupId.
    /// </summary>
    private Dictionary<uint, Completion> CompletionGroupsById = new();
    private TrieDictionary<List<CompletionInfo>> CompletionsByText = new(new AccentInsensitiveCharComparer());
    private Dictionary<(uint, uint), CompletionInfo> CompletionInfoByGroupKey = new();

    public enum IndexState { UNINDEXED, INDEXING, INDEXED }
    public IndexState State { get; private set; } = IndexState.UNINDEXED;

    public IEnumerable<CompletionInfo> Search(string prefix) {
        if (State != IndexState.INDEXED) { return new List<CompletionInfo>(); }
        if (prefix == "") { return new List<CompletionInfo>(); }

        return CompletionsByText.StartsWith(prefix).SelectMany(c => c.Value);
    }

    public CompletionInfo? ById(uint group, uint key) {
        if (State != IndexState.INDEXED) { return null; }

        return CompletionInfoByGroupKey.GetValueOrDefault((group, key));
    }

    public CompletionIndex() {
        StartIndexing();
    }

    public void StartIndexing() {
        if (State != IndexState.UNINDEXED) { return; }

        Task.Run(() => {
            try {
                State = IndexState.INDEXING;
                RefreshCompletionGroupIndex();

                var completions = AllCompletionInfo();
                RefreshCompletionIndex(completions);
                RefreshCompletionInfoByGroupKey(completions);
                State = IndexState.INDEXED;
            } catch (Exception ex) {
                Env.PluginLog.Error($"Failed to index completions\n{ex}");
            }
        });
    }

    private void RefreshCompletionGroupIndex() {
        var completionGroups = Env.DataManager.GetExcelSheet<Completion>()
            .Where(c => {
                var lookupTable = c.LookupTable.ExtractText();
                return lookupTable != "";
            });

        foreach (var cg in completionGroups) {
            CompletionGroupsById[cg.Group] = cg;
        }
    }

    private List<CompletionInfo> AllCompletionInfo() {
        return Env.DataManager.GetExcelSheet<Completion>()
             .Select(raw => ParsedCompletion.From(raw))
             .SelectMany<ParsedCompletion, CompletionInfo>(parsed => CompletionInfo.From(parsed))
             .Select(info => {
                 if (CompletionGroupsById.TryGetValue(info.Group, out var completionGroup)) {
                     return info with { GroupTitle = completionGroup.GroupTitle };
                 }
                 return info;
             })
            .ToList();
    }

    private void RefreshCompletionIndex(IEnumerable<CompletionInfo> completions) {
        var grouped = completions.GroupBy(c => c.SeString.ExtractText(), new AccentInsensitiveStringComparer());
        foreach (var g in grouped) {
            CompletionsByText.Add(g.Key, g.ToList());
        }
    }

    private void RefreshCompletionInfoByGroupKey(IEnumerable<CompletionInfo> completions) {
        foreach (var completion in completions) {
            CompletionInfoByGroupKey[(completion.Group, completion.Key)] = completion;
        }
    }
}
