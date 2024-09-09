
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.Dalamud.Str;
using MacroMate.Extensions.Markdig;
using MacroMate.MacroTree;
using Markdig;
using Markdig.Syntax;

namespace MacroMate.Subscription;

/// <summary>
/// Class to manage subscription groups, including downloading stuff and updating the tree.
/// </summary>
public class SubscriptionManager {
    private static readonly TimeSpan CheckForUpdatesTimeAfterLogin = TimeSpan.FromSeconds(20);

    private List<MateNode.SubscriptionGroup> SubscriptionGroups  = new();

    private ConcurrentDictionary<Guid, Task> SubscriptionGroupTasks = new();

    private ConcurrentDictionary<Guid, SubscriptionTaskDetails> SubscriptionGroupTaskDetails = new();

    private bool firstLogin = true;
    private DateTimeOffset? lastAutoCheckForUpdatesTime = null;
    private DateTimeOffset? nextAutoCheckForUpdatesTime = null;

    public SubscriptionManager() {
        Env.MacroConfig.ConfigChange += OnMacroMateConfigChanged;
        Env.Framework.Update += this.OnFrameworkUpdate;

        // If we are already logged run the "First Login" to make plugin reloads consistent with
        // first login.
        if (Env.ClientState.LocalPlayer != null) {
            OnLogin();
        }


        Env.ClientState.Login += OnLogin;

        OnMacroMateConfigChanged();
    }

    public void ScheduleCheckForUpdates(MateNode.SubscriptionGroup sGroup) {
        if (SubscriptionGroupTasks.ContainsKey(sGroup.Id)) { return; }

        SubscriptionGroupTasks[sGroup.Id] = Task.Run(async () => {
            try {
                await CheckForUpdates(sGroup);
            } finally {
                SubscriptionGroupTasks.Remove(sGroup.Id, out var _);
            }
        });
    }

    public void ScheduleSync(MateNode.SubscriptionGroup sGroup) {
        if (SubscriptionGroupTasks.ContainsKey(sGroup.Id)) { return; }

        SubscriptionGroupTasks[sGroup.Id] = Task.Run(async () => {
            try {
                await Sync(sGroup);
            } finally {
                SubscriptionGroupTasks.Remove(sGroup.Id, out var _);
            }
        });
    }

    public SubscriptionTaskDetails GetSubscriptionTaskDetails(MateNode.SubscriptionGroup sGroup) {
        return SubscriptionGroupTaskDetails.GetOrAdd(sGroup.Id, new SubscriptionTaskDetails());
    }

    private void OnLogin() {
        if (firstLogin) {
            nextAutoCheckForUpdatesTime = DateTimeOffset.Now + CheckForUpdatesTimeAfterLogin;
            firstLogin = false;
        }
    }

    private void OnMacroMateConfigChanged() {
        // If the config has changed the tree might have changed, we need to refresh
        // our subscription groups.
        SubscriptionGroups = Env.MacroConfig.Root.Descendants()
            .OfType<MateNode.SubscriptionGroup>()
            .ToList();

        // Only set this if we've done at least one auto-update to give the on-login check time to run
        if (lastAutoCheckForUpdatesTime != null) {
            nextAutoCheckForUpdatesTime = lastAutoCheckForUpdatesTime +
                TimeSpan.FromMinutes(Env.MacroConfig.MinutesBetweenSubscriptionAutoCheckForUpdates);
        }
    }

    private void OnFrameworkUpdate(IFramework framework) {
        if (!Env.MacroConfig.EnableSubscriptionAutoCheckForUpdates) { return; }
        if (SubscriptionGroups.Count == 0) { return; }
        if (nextAutoCheckForUpdatesTime == null) { return; }

        if (DateTimeOffset.Now > nextAutoCheckForUpdatesTime) {
            Env.PluginLog.Info("Checking subscriptions for updates");
            foreach (var sGroup in SubscriptionGroups) {
                if (sGroup.HasUpdate) { continue; }
                ScheduleCheckForUpdates(sGroup);
            }
            lastAutoCheckForUpdatesTime = DateTimeOffset.Now;
            nextAutoCheckForUpdatesTime = nextAutoCheckForUpdatesTime + TimeSpan.FromMinutes(Env.MacroConfig.MinutesBetweenSubscriptionAutoCheckForUpdates);
        }
    }

    private async Task Sync(MateNode.SubscriptionGroup sGroup) {
        var taskDetails = SubscriptionGroupTaskDetails[sGroup.Id] = new SubscriptionTaskDetails();
        await taskDetails.Catching(async () => {
            var etags = new ConcurrentBag<string>();
            var (manifest, _) = await FetchManifest(sGroup, etags, taskDetails);

            await Env.Framework.RunOnTick(() => { sGroup.Name = manifest.Name; });

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 2 };
            await Parallel.ForEachAsync(manifest.Macros, parallelOptions, async (macroYaml, token) => {
                await SyncMacro(sGroup, macroYaml, etags, taskDetails);
                await CheckMacroForUpdates(sGroup, macroYaml, etags, taskDetails);
            });

            await Env.Framework.RunOnTick(() => {
                sGroup.LastSyncTime = DateTimeOffset.Now;
                sGroup.LastSyncETags.Clear();
                sGroup.LastSyncETags.AddRange(etags);
                sGroup.HasUpdate = false;
                Env.MacroConfig.NotifyEdit();
            });
        });
    }

    private async Task SyncMacro(
        MateNode.SubscriptionGroup sGroup,
        MacroYAML macroYaml,
        ConcurrentBag<string> etags,
        SubscriptionTaskDetails taskDetails
    ) {
        var parsedParentPath = MacroPath.ParseText(macroYaml.Group ?? "/");
        var parent = Env.MacroConfig.CreateOrFindGroupByPath(sGroup, parsedParentPath);
        var macro = Env.MacroConfig.CreateOrFindMacroByName(macroYaml.Name!, parent);

        var childDetails = taskDetails.Child($"Sync '{parsedParentPath}/{macro.Name}'");
        await SyncOrCheckMacro(sGroup, macroYaml, macro, etags, childDetails, sync: true);
    }

    private async Task SyncOrCheckMacro(
        MateNode.SubscriptionGroup sGroup,
        MacroYAML macroYaml,
        MateNode.Macro macro,
        ConcurrentBag<string> etags,
        SubscriptionTaskDetails taskDetails,
        bool sync
    ) {
        var updatedFields = new List<string>();

        var newIconId = (uint?)macroYaml.IconId ?? VanillaMacro.DefaultIconId;
        if (macro.IconId != newIconId) { updatedFields.Add("icon id"); }

        var newNotes = macroYaml.Notes ?? "";
        if (macro.Notes != newNotes) { updatedFields.Add("notes"); }

        SeString newLines;
        if (macroYaml.Lines != null) {
            newLines = SeStringEx.ParseFromText(macroYaml.Lines);
            if (!macro.Lines.IsSame(newLines)) { updatedFields.Add("lines"); }
        } else {
            var (mdLines, mdLinesModified) = await FetchMacroMarkdownLinesIfModified(sGroup, macroYaml, macro, etags, taskDetails);
            if (mdLinesModified) { updatedFields.Add("lines"); }
            newLines = mdLines ?? "";
        }

        if (updatedFields.Count > 0) {
            var fields = string.Join(", ", updatedFields);
            taskDetails.Child($"Has changes - {fields} modified");
            if (sync) {
                await Env.Framework.RunOnTick(() => {
                    macro.IconId = newIconId;
                    macro.Notes = newNotes;
                    macro.Lines = newLines;
                });
            } else {
                await Env.Framework.RunOnTick(() => { sGroup.HasUpdate = true; });
            }
            return;
        } else {
            taskDetails.Child($"No updates");
        }
    }

    private async Task CheckForUpdates(MateNode.SubscriptionGroup sGroup) {
        var taskDetails = SubscriptionGroupTaskDetails[sGroup.Id] = new SubscriptionTaskDetails();
        await taskDetails.Catching(async () => {
            var etags = new ConcurrentBag<string>();
            var (manifest, manifestUpdated) = await FetchManifest(sGroup, etags, taskDetails);

            if (manifestUpdated == true) {
                await Env.Framework.RunOnTick(() => { sGroup.HasUpdate = true; });
                return;
            }

            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3 };
            await Parallel.ForEachAsync(manifest.Macros, parallelOptions, async (macroYaml, token) => {
                await CheckMacroForUpdates(sGroup, macroYaml, etags, taskDetails);
            });

            await Env.Framework.RunOnTick(() => { sGroup.HasUpdate = false; });
        });
    }

    private async Task CheckMacroForUpdates(
        MateNode.SubscriptionGroup sGroup,
        MacroYAML macroYaml,
        ConcurrentBag<string> etags,
        SubscriptionTaskDetails taskDetails
    ) {
        var childDetails = taskDetails.Child($"Check '{macroYaml.Name}'");

        var parsedParentPath = MacroPath.ParseText(macroYaml.Group ?? "/");
        var existingParent = sGroup.Walk(parsedParentPath);
        if (existingParent == null) {
            childDetails.Child($"Has update - New Group '{parsedParentPath}'");
            await Env.Framework.RunOnTick(() => { sGroup.HasUpdate = true; });
            return;
        }

        var existingMacro = existingParent.Children
            .OfType<MateNode.Macro>()
            .FirstOrDefault(child => child.Name == (macroYaml.Name ?? ""));
        if (existingMacro == null) {
            childDetails.Child($"Has update - New Macro '{macroYaml.Name}'");
            await Env.Framework.RunOnTick(() => { sGroup.HasUpdate = true; });
            return;
        }

        await SyncOrCheckMacro(sGroup, macroYaml, existingMacro, etags, childDetails, sync: false);
    }

    private async Task<(SubscriptionManifestYAML, bool)> FetchManifest(
        MateNode.SubscriptionGroup sGroup,
        ConcurrentBag<string> etags,
        SubscriptionTaskDetails taskDetails
    ) {
        var childTask = taskDetails.Child("Fetch Manifest");

        // If we already know we have an update, there's nothing to do
        var response = await childTask.Child("Downloading Headers").Loading(async () => {
            var request = new HttpRequestMessage(HttpMethod.Get, sGroup.SubscriptionUrl);
            var response = await Env.HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return response;
        });

        if (response.Headers.ETag != null) { etags.Add(response.Headers.ETag.Tag); }

        var manifestStr = await response.Content.ReadAsStringAsync();
        var manifest = SubscriptionManifestYAML.From(manifestStr);

        if (response.Headers.ETag != null && !sGroup.LastSyncETags.Contains(response.Headers.ETag.Tag)) {
            childTask.Child($"Has changes - ETag mismatch {response.Headers.ETag.Tag}");
            return (manifest, true);
        } else {
            childTask.Child("No changes - ETag match");
            return (manifest, false);
        }
    }


    /// <summary>
    /// Fetches the markdown lines for a given macro (if available and updated)
    /// </summary>
    private async Task<(string?, bool)> FetchMacroMarkdownLinesIfModified(
        MateNode.SubscriptionGroup sGroup,
        MacroYAML macroYaml,
        MateNode.Macro existingMacro,
        ConcurrentBag<string> etags,
        SubscriptionTaskDetails taskDetails
    ) {
        // If we don't have a MarkdownUrl there aren't any additional checks to make
        if (macroYaml.MarkdownUrl == null) {
            return (null, false);
        }

        // If we have a markdown URL we want to use it to check if there are any additional updates
        var url = sGroup.RelativeUrl(macroYaml.MarkdownUrl);
        var response = await taskDetails.Child("Download Markdown Headers").Loading(async () => {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await Env.HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return response;
        });

        if (response.Headers.ETag != null) { etags.Add(response.Headers.ETag.Tag); }

        // If we have an etag and it already matches then we know this file doesn't have any updates
        if (response.Headers.ETag != null && sGroup.LastSyncETags.Contains(response.Headers.ETag.Tag)) {
            taskDetails.Child("No changes - ETag match");
            return (null, false);
        } else {
            taskDetails.Child($"May have changes - ETag mismatch {response.Headers.ETag?.Tag}");
        }

        // If we didn't get an etag match we need to check the markdown content against our own to see
        // if there is a change.
        var mdString = await taskDetails.Child("Downloading Markdown").Loading(async () => {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await Env.HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        });

        var mdLines = ExtractMacroLinesFromMarkdown(mdString, macroYaml);
        if (mdLines != null) {
            var mdSeString = SeStringEx.ParseFromText(mdLines);
            if (mdSeString.IsSame(existingMacro.Lines)) {
                taskDetails.Child("No changes - markdown lines unmodified");
                return (mdLines, false);
            } else {
                taskDetails.Child("Has changes - markdown lines modified");
                return (mdLines, true);
            }
        } else {
            // Otherwise we couldn't find any Macro code lines, so this is a bust
            return (null, false);
        }
    }

    private string? ExtractMacroLinesFromMarkdown(string markdownString, MacroYAML macroYaml) {
        var mdDoc = Markdown.Parse(markdownString);

        var macroCodeBlock = mdDoc.Descendants<CodeBlock>()
            .Take((macroYaml.MarkdownMacroCodeBlockIndex ?? 0) + 1)
            .LastOrDefault();
        var lines = macroCodeBlock?.ToRawLines();
        return lines;
    }
}
