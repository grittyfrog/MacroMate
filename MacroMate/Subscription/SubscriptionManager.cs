
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.Dalamud.Str;
using MacroMate.MacroTree;

namespace MacroMate.Subscription;

/// <summary>
/// Class to manage subscription groups, including downloading stuff and updating the tree.
/// </summary>
public class SubscriptionManager {
    /// <summary>
    /// Time we should wait after login to update.
    /// </summary>
    private static readonly TimeSpan UpdateTimeAfterLogin = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Time we should wait between scheduled update checks.
    /// </summary>
    private static readonly TimeSpan TimeBetweenUpdateChecks = TimeSpan.FromHours(2);

    private List<MateNode.SubscriptionGroup> SubscriptionGroups  = new();

    private ConcurrentDictionary<Guid, Task> SubscriptionGroupTasks = new();

    // Class used to communicate the state of subscription processing for specific SubscriptionGroup
    private ConcurrentDictionary<Guid, SubscriptionState> SubscriptionStates = new();

    public SubscriptionManager() {
        Env.MacroConfig.ConfigChange += OnMacroMateConfigChanged;

        OnMacroMateConfigChanged();
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
    }

    private async Task CheckForUpdates(MateNode.SubscriptionGroup sGroup) {
        var state = GetSubscriptionState(sGroup);
        state.Reset();
        try {
            var etags = new List<string>();
            var response = await AddUrlETag("Checking Macro Manifest", sGroup.SubscriptionUrl, state, etags);

            var readManifestStep = state.InProgress("Read Macro Manifest");
            var manifestStr = await response.Content.ReadAsStringAsync();
            var manifest = SubscriptionManifestYAML.From(manifestStr);
            readManifestStep.State = SubscriptionState.StepState.SUCCESS;

            foreach (var macroYaml in manifest.Macros) {
                var url = macroYaml.MarkdownUrl;
                if (url == null) { continue; }

                await AddUrlETag($"Checking {url}", url, state, etags);
            }

            await Env.Framework.RunOnTick(() => {
                sGroup.KnownRemoteETags.Clear();
                sGroup.KnownRemoteETags.AddRange(etags);
                var msg = sGroup.HasUpdates() ? "Found updates" : "Up to date";
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
                var yamlGroupStr = macroYaml.Group ?? "/";
                var syncMacroStep = state.InProgress($"Sync '{yamlGroupStr}/{macroYaml.Name}'");
                var parsedParentPath = MacroPath.ParseText(yamlGroupStr);
                var parent = Env.MacroConfig.CreateOrFindGroupByPath(sGroup, parsedParentPath);
                var macro = Env.MacroConfig.CreateOrFindMacroByName(macroYaml.Name!, parent);
                syncMacroStep.State = SubscriptionState.StepState.SUCCESS;

                await Env.Framework.RunOnTick(() => {
                    macro.IconId = (uint?)macroYaml.IconId ?? VanillaMacro.DefaultIconId;
                    macro.Lines = SeStringEx.ParseFromText(macroYaml.Lines ?? "");
                });
            }

            await Env.Framework.RunOnTick(() => {
                sGroup.LastSyncTime = DateTimeOffset.Now;
                sGroup.LastSyncETags.Clear();
                sGroup.LastSyncETags.AddRange(etags);
                sGroup.KnownRemoteETags.Clear();
                sGroup.KnownRemoteETags.AddRange(etags);
                Env.MacroConfig.NotifyEdit();
            });
        } catch (Exception ex) {
            Env.PluginLog.Error($"Failed to sync subscription updates: {ex.Message}\n{ex.StackTrace}");
            state.FailLast(ex.Message);
        } finally {
            SubscriptionGroupTasks.Remove(sGroup.Id, out _);
        }
    }
}
