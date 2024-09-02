
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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
    /// <summary>
    /// Time we should wait after login to check for updates.
    /// </summary>
    private static readonly TimeSpan CheckForUpdatesTimeAfterLogin = TimeSpan.FromSeconds(20);

    private List<MateNode.SubscriptionGroup> SubscriptionGroups  = new();

    private ConcurrentDictionary<Guid, Task> SubscriptionGroupTasks = new();

    // Class used to communicate the state of subscription processing for specific SubscriptionGroup
    private ConcurrentDictionary<Guid, SubscriptionState> SubscriptionStates = new();

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

    private void OnLogin() {
        if (firstLogin) {
            nextAutoCheckForUpdatesTime = DateTimeOffset.Now + CheckForUpdatesTimeAfterLogin;
            firstLogin = false;
        }
    }

    public SubscriptionState GetSubscriptionState(MateNode.SubscriptionGroup sGroup) {
        return SubscriptionStates.GetOrAdd(sGroup.Id, (_) => new SubscriptionState());
    }

    public void ScheduleCheckForUpdates(MateNode.SubscriptionGroup sGroup) {
        if (SubscriptionGroupTasks.ContainsKey(sGroup.Id)) { return; }

        SubscriptionGroupTasks[sGroup.Id] = CheckForUpdates(sGroup);
    }

    public void ScheduleSyncFromSubscription(MateNode.SubscriptionGroup sGroup) {
        if (SubscriptionGroupTasks.ContainsKey(sGroup.Id)) { return; }

        SubscriptionGroupTasks[sGroup.Id] = SyncFromSubscription(sGroup);
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
                ScheduleCheckForUpdates(sGroup);
            }
            lastAutoCheckForUpdatesTime = DateTimeOffset.Now;
            nextAutoCheckForUpdatesTime = nextAutoCheckForUpdatesTime + TimeSpan.FromMinutes(Env.MacroConfig.MinutesBetweenSubscriptionAutoCheckForUpdates);
        }
    }

    private async Task CheckForUpdates(MateNode.SubscriptionGroup sGroup) {
        var state = GetSubscriptionState(sGroup);
        state.Reset();
        try {
            var hasUpdate = false;
            var etags = new List<string>();
            var response = await AddUrlETag("Checking Macro Manifest", sGroup.SubscriptionUrl, state, etags);

            var readManifestStep = state.InProgress("Read Macro Manifest");
            var manifestStr = await response.Content.ReadAsStringAsync();
            var manifest = SubscriptionManifestYAML.From(manifestStr);
            readManifestStep.State = SubscriptionState.StepState.SUCCESS;

            foreach (var macroYaml in manifest.Macros) {
                if (macroYaml.MarkdownUrl == null) { continue; }

                var url = sGroup.RelativeUrl(macroYaml.MarkdownUrl);
                var mdResponse = await AddUrlETag($"Checking {url}", url, state, etags);
            }

            await Env.Framework.RunOnTick(() => {
                sGroup.HasUpdate = hasUpdate;
                var msg = sGroup.HasUpdate ? "Found updates" : "Up to date";
                state.Info(msg);

                Env.MacroConfig.NotifyEdit();
            });
        } catch (Exception ex) {
            Env.PluginLog.Error($"Failed to check for subscription updates: {ex.Message}\n{ex.StackTrace}");
            state.FailLast(ex.Message);
        } finally {
            SubscriptionGroupTasks.Remove(sGroup.Id, out _);
        }

    }

    private async Task<HttpResponseMessage> AddUrlETag(
        string message,
        string url,
        SubscriptionState state,
        List<string> etags
    ) {
        var step = state.InProgress(message);
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var response = await Env.HttpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        step.State = SubscriptionState.StepState.SUCCESS;

        if (response.Headers.ETag == null) {
            step.FailMessage = "response did not contain ETag, cannot check for updates";
            step.State = SubscriptionState.StepState.FAILED;
            return response;
        }

        var etag = response.Headers.ETag.Tag;
        etags.Add(etag);

        return response;
    }

    private async Task SyncFromSubscription(MateNode.SubscriptionGroup sGroup) {
        var state = GetSubscriptionState(sGroup);
        state.Reset();
        try {
            var etags = new List<string>();
            var fetchManifestStep = state.InProgress("Fetch Macro Manifest");
            var request = new HttpRequestMessage(HttpMethod.Get, sGroup.SubscriptionUrl);
            var response = await Env.HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            fetchManifestStep.State = SubscriptionState.StepState.SUCCESS;
            if (response.Headers.ETag != null) {
                etags.Add(response.Headers.ETag.Tag);
            }

            var readManifestStep = state.InProgress("Read Macro Manifest");
            var manifestStr = await response.Content.ReadAsStringAsync();
            var manifest = SubscriptionManifestYAML.From(manifestStr);
            readManifestStep.State = SubscriptionState.StepState.SUCCESS;

            sGroup.Name = manifest.Name;
            foreach (var macroYaml in manifest.Macros) {
                await SyncMacro(macroYaml, state, sGroup, etags);
            }

            await Env.Framework.RunOnTick(() => {
                sGroup.LastSyncTime = DateTimeOffset.Now;
                sGroup.LastSyncETags.Clear();
                sGroup.LastSyncETags.AddRange(etags);
                sGroup.HasUpdate = false;
                Env.MacroConfig.NotifyEdit();
            });
        } catch (Exception ex) {
            Env.PluginLog.Error($"Failed to sync subscription updates: {ex.Message}\n{ex.StackTrace}");
            state.FailLast(ex.Message);
        } finally {
            SubscriptionGroupTasks.Remove(sGroup.Id, out _);
        }
    }

    private async Task SyncMacro(MacroYAML macroYaml, SubscriptionState state, MateNode.SubscriptionGroup sGroup, List<string> etags) {
        string name = "<Missing Name>";
        string macroGroup = "/";
        uint iconId = VanillaMacro.DefaultIconId;
        string lines = "";
        string notes = "";

        // If we have a markdown URL we want to grab it first and use it as our "base" fields
        if (macroYaml.MarkdownUrl != null) {
            var url = sGroup.RelativeUrl(macroYaml.MarkdownUrl);
            var fetchStep = state.InProgress($"Fetch '{url}'");
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await Env.HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var mdString = await response.Content.ReadAsStringAsync();
            fetchStep.State = SubscriptionState.StepState.SUCCESS;
            if (response.Headers.ETag != null) {
                etags.Add(response.Headers.ETag.Tag);
            }

            var parseStep = state.InProgress($"Parse Markdown from '{url}'");

            var mdDoc = Markdown.Parse(mdString);

            var macroCodeBlock = mdDoc.Descendants<CodeBlock>()
                .Take((macroYaml.MarkdownMacroCodeBlockIndex ?? 0) + 1)
                .LastOrDefault();
            if (macroCodeBlock != null) {
                lines = macroCodeBlock.ToRawLines();
            }

            parseStep.State = SubscriptionState.StepState.SUCCESS;
        }

        if (macroYaml.Name != null) { name = macroYaml.Name; }
        if (macroYaml.Group != null) { macroGroup = macroYaml.Group; }
        if (macroYaml.IconId != null) { iconId = (uint)macroYaml.IconId; }
        if (macroYaml.Lines != null) { lines = macroYaml.Lines; }
        if (macroYaml.Notes != null) { notes = macroYaml.Notes; }

        var syncMacroStep = state.InProgress($"Sync '{macroGroup}/{name}'");
        var parsedParentPath = MacroPath.ParseText(macroGroup);
        var parent = Env.MacroConfig.CreateOrFindGroupByPath(sGroup, parsedParentPath);
        var macro = Env.MacroConfig.CreateOrFindMacroByName(macroYaml.Name!, parent);
        syncMacroStep.State = SubscriptionState.StepState.SUCCESS;

        await Env.Framework.RunOnTick(() => {
            macro.IconId = (uint?)macroYaml.IconId ?? VanillaMacro.DefaultIconId;
            macro.Notes = notes;
            macro.Lines = SeStringEx.ParseFromText(lines);
        });
    }
}
