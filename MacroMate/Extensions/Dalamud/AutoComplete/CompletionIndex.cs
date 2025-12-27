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
    private Dictionary<uint, Completion> completionGroupsById = new();
    private TrieDictionary<List<CompletionInfo>> completionsByText = new(new AccentInsensitiveCharComparer());
    private Dictionary<(uint, uint), CompletionInfo> completionInfoByGroupKey = new();

    public enum IndexState { UNINDEXED, INDEXING, INDEXED }
    public IndexState State { get; private set; } = IndexState.UNINDEXED;

    public IEnumerable<CompletionInfo> Search(string prefix) {
        if (State != IndexState.INDEXED) { return new List<CompletionInfo>(); }
        if (prefix == "") { return new List<CompletionInfo>(); }

        return completionsByText
            .Where(entry => entry.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) // 找到所有键以prefix开头的条目
            .SelectMany(entry => entry.Value); // 展开所有的CompletionInfo列表
    }

    public CompletionInfo? ById(uint group, uint key) {
        if (State != IndexState.INDEXED) { return null; }

        return completionInfoByGroupKey.GetValueOrDefault((group, key));
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
                RefreshcompletionInfoByGroupKey(completions);
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
            completionGroupsById[cg.Group] = cg;
        }
    }

    private List<CompletionInfo> AllCompletionInfo() {
        // ===== 新增：全表扫描诊断 =====
        var allRawCompletions = Env.DataManager.GetExcelSheet<Completion>().ToList(); // 先物化列表以便统计
        Env.PluginLog.Warning($"[全表扫描] 开始扫描，共获取到 {allRawCompletions.Count} 条原始数据");

        int totalCount = 0;
        int atSymbolCount = 0;
        int emptyCount = 0;
        int otherFormatCount = 0;

        foreach (var raw in allRawCompletions)
        {
            totalCount++;
            var rawLookupText = raw.LookupTable.ExtractText();
            var evaluatedTable = Env.SeStringEvaluator.Evaluate(raw.LookupTable).ExtractText();

            // 统计并记录非标准格式
            if (string.IsNullOrWhiteSpace(evaluatedTable))
            {
                emptyCount++;
                if (emptyCount <= 2)
                { // 只记录前2个例子
                    Env.PluginLog.Warning($"[全表扫描-空值] RowId={raw.RowId}, Key={raw.Key}, Group={raw.Group}, Text='{raw.Text.ExtractText()}'");
                }
            }
            else if (evaluatedTable == "@")
            {
                atSymbolCount++;
                if (atSymbolCount <= 2)
                { // 只记录前2个例子
                    Env.PluginLog.Warning($"[全表扫描-@符号] RowId={raw.RowId}, Key={raw.Key}, Group={raw.Group}, Text='{raw.Text.ExtractText()}'");
                }
            }
            else
            {
                otherFormatCount++;
                // 重点：记录所有非空非@的格式，这些是可能有用的数据
                Env.PluginLog.Warning($"[全表扫描-其他格式] RowId={raw.RowId}, Key={raw.Key}, Group={raw.Group}, Text='{raw.Text.ExtractText()}', LookupTable原始='{rawLookupText}', 解析后='{evaluatedTable}'");
            }
        }

        Env.PluginLog.Warning($"[全表扫描] 统计结果: 总计={totalCount}, @符号={atSymbolCount}, 空值={emptyCount}, 其他格式={otherFormatCount}");
        // ===== 诊断结束 =====

        // 下面是原有的业务逻辑，现在使用我们刚刚遍历过的列表
        return allRawCompletions
            .Select(raw => ParsedCompletion.From(raw))
            .SelectMany<ParsedCompletion, CompletionInfo>(parsed => CompletionInfo.From(parsed))
            .Select(info => {
                if (completionGroupsById.TryGetValue(info.Group, out var completionGroup))
                {
                    return info with { GroupTitle = completionGroup.GroupTitle };
                }
                return info;
            })
            .ToList();
    }

    private void RefreshCompletionIndex(IEnumerable<CompletionInfo> completions)
    {
        // 修复：过滤掉提取文本为 null 或空白的条目
        var validCompletions = completions.Where(c => !string.IsNullOrWhiteSpace(c.SeString.ExtractText()));
        var grouped = completions.GroupBy(c => c.SeString.ExtractText(), new AccentInsensitiveStringComparer());
        foreach (var g in grouped)
        {
            // 额外的安全检查
            if (!string.IsNullOrWhiteSpace(g.Key))
            {
                completionsByText.Add(g.Key, g.ToList());
            }
        }
    }

    private void RefreshcompletionInfoByGroupKey(IEnumerable<CompletionInfo> completions) {
        foreach (var completion in completions) {
            completionInfoByGroupKey[(completion.Group, completion.Key)] = completion;
        }
    }
}
