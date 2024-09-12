
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
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

    private SemaphoreSlim JobSemaphore = new SemaphoreSlim(1, 1);

    private CancellationTokenSource CancellationTokenSource = new();

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
        await taskDetails.Child("Waiting for existing jobs to finish").Loading(async () => {
            await JobSemaphore.WaitAsync();
        });
        try {
            await taskDetails.Catching(async () => {
                var urlToEtags = new ConcurrentDictionary<string, string>();
                var (manifest, _) = await FetchManifest(sGroup, urlToEtags, taskDetails);

                await Env.Framework.RunOnTick(() => { sGroup.Name = manifest.Name; });

                // We create the groups / macros first non-async to avoid race conditions where
                // groups are created multiple times.
                var macroYamlAndMacro = manifest.Macros.Select(macroYaml => {
                    var parsedParentPath = MacroPath.ParseText(macroYaml.Group ?? "/");
                    var parent = Env.MacroConfig.CreateOrFindGroupByPath(sGroup, parsedParentPath);
                    var macro = Env.MacroConfig.CreateOrFindMacroByName(macroYaml.Name!, parent);
                    return (macroYaml, macro);
                });

                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };
                await Parallel.ForEachAsync(macroYamlAndMacro, parallelOptions, async (macroYamlAndMacro, token) => {
                    var (macroYaml, macro) = macroYamlAndMacro;

                    var childDetails = taskDetails.Child($"Sync '{macro.PathString}'");
                    await SyncOrCheckMacro(sGroup, macroYaml, macro, urlToEtags, childDetails, sync: true);
                });

                await Env.Framework.RunOnTick(() => {
                    sGroup.LastSyncTime = DateTimeOffset.Now;
                    sGroup.HasUpdate = false;
                    Env.MacroConfig.SubscriptionUrlCache.ClearForSubscription(sGroup.Id);
                    Env.MacroConfig.SubscriptionUrlCache.AddEntries(sGroup, urlToEtags);
                    Env.MacroConfig.NotifyEdit();
                });
            });
        } finally {
            JobSemaphore.Release();
        }
    }

    private async Task SyncOrCheckMacro(
        MateNode.SubscriptionGroup sGroup,
        MacroYAML macroYaml,
        MateNode.Macro macro,
        ConcurrentDictionary<string, string> urlToEtags,
        SubscriptionTaskDetails taskDetails,
        bool sync
    ) {
        var updatedFields = new List<string>();

        var newIconId = (uint?)macroYaml.IconId ?? VanillaMacro.DefaultIconId;
        if (macro.IconId != newIconId) { updatedFields.Add("icon id"); }

        var newNotes = macroYaml.Notes ?? "";
        if (macro.Notes != newNotes) { updatedFields.Add("notes"); }

        SeString newLines;
        if (macroYaml.MarkdownUrl != null) {
            var (mdLines, mdLinesModified) = await FetchMacroMarkdownLinesIfModified(sGroup, macroYaml, macro, urlToEtags, taskDetails);
            if (mdLinesModified) { updatedFields.Add("lines"); }
            newLines = mdLines ?? "";
        } else {
            newLines = SeStringEx.ParseFromText(macroYaml.Lines ?? "");
            if (!macro.Lines.IsSame(newLines)) { updatedFields.Add("lines"); }
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
            taskDetails.Child($"No changes");
        }
    }

    private async Task CheckForUpdates(MateNode.SubscriptionGroup sGroup) {
        var taskDetails = SubscriptionGroupTaskDetails[sGroup.Id] = new SubscriptionTaskDetails();
        await taskDetails.Child("Waiting for existing jobs to finish").Loading(async () => {
            await JobSemaphore.WaitAsync();
        });
        try {
            await taskDetails.Catching(async () => {
                var urlToEtags = new ConcurrentDictionary<string, string>();
                var (manifest, manifestUpdated) = await FetchManifest(sGroup, urlToEtags, taskDetails);

                if (manifestUpdated == true) {
                    await Env.Framework.RunOnTick(() => { sGroup.HasUpdate = true; });
                    return;
                }

                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };
                await Parallel.ForEachAsync(manifest.Macros, parallelOptions, async (macroYaml, token) => {
                    await CheckMacroForUpdates(sGroup, macroYaml, urlToEtags, taskDetails);
                });

                await Env.Framework.RunOnTick(() => { sGroup.HasUpdate = false; });
            });
        } finally {
            JobSemaphore.Release();
        }
    }

    private async Task CheckMacroForUpdates(
        MateNode.SubscriptionGroup sGroup,
        MacroYAML macroYaml,
        ConcurrentDictionary<string, string> urlToEtags,
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

        await SyncOrCheckMacro(sGroup, macroYaml, existingMacro, urlToEtags, childDetails, sync: false);
    }

    private async Task<(SubscriptionManifestYAML, bool)> FetchManifest(
        MateNode.SubscriptionGroup sGroup,
        ConcurrentDictionary<string, string> urlToEtags,
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

        var hasKnownEtag = Env.MacroConfig.SubscriptionUrlCache.TryGetEtagForUrl(sGroup.SubscriptionUrl, out var knownEtag);

        if (response.Headers.ETag != null) { urlToEtags[sGroup.SubscriptionUrl] = response.Headers.ETag.Tag; }

        var manifestStr = await response.Content.ReadAsStringAsync();
        var manifest = SubscriptionManifestYAML.From(manifestStr);

        if (response.Headers.ETag != null && !hasKnownEtag) {
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
        ConcurrentDictionary<string, string> urlToEtags,
        SubscriptionTaskDetails taskDetails
    ) {
        // If we don't have a MarkdownUrl there aren't any additional checks to make
        if (macroYaml.MarkdownUrl == null) {
            return (null, false);
        }

        // If we have a markdown URL we want to use it to check if there are any additional updates
        var url = sGroup.RelativeUrl(macroYaml.MarkdownUrl);
        var response = await taskDetails.Child("Checking Markdown Headers").Loading(async () => {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (Env.MacroConfig.SubscriptionUrlCache.TryGetEtagForUrl(url, out var knownEtag)) {
                request.Headers.IfNoneMatch.ParseAdd(knownEtag);
            }

            var response = await Env.HttpClient.SendAsync(request);
            return response;
        });

        if (response.Headers.ETag != null) { urlToEtags[url] = response.Headers.ETag.Tag; }

        // If we got a 304 we know the ETag matches and no changes have taken place since we last synced
        if (response.StatusCode == HttpStatusCode.NotModified) {
            taskDetails.Child("No changes - ETag match");
            return (null, false);
        } else {
            response.EnsureSuccessStatusCode();
            taskDetails.Child($"May have changes - ETag mismatch {response.Headers.ETag?.Tag}");
        }

        // If we didn't get an etag match we need to check the markdown content against our own to see
        // if there is a change.
        var mdString = await taskDetails.Child("Downloading Markdown").Loading(async () => {
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
