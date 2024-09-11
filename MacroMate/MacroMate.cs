using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using MacroMate.Conditions;
using MacroMate.Extensions.Dalamud.Macros;

namespace MacroMate;

public class MacroMate {
    /// We need to track CurrentLinks so we can know when we unlink something and need to delete it
    private ImmutableList<VanillaMacroLink> CurrentVanillaLinks;

    public MacroMate() {
        // Now that we're initialized we can load the save file.
        LoadConfig();

        Env.ConditionManager.ConditionChange += OnConditionChange;
        Env.MacroConfig.ConfigChange += OnConfigChange;

        CurrentVanillaLinks = Env.MacroConfig.AllVanillaLinks();
        Refresh(Env.ConditionManager.CurrentConditions());
    }

    public bool CanLoadConfig() => Env.SaveManager.CanLoad();

    public void LoadConfig() {
        var savedConfig = Env.SaveManager.Load();
        if (savedConfig != null) {
            Env.MacroConfig.OverwiteFrom(savedConfig);
            Env.MacroConfig.SubscriptionUrlCache.ClearUnused();
        }
    }

    public void LoadBackup(FileInfo backup) {
        var backupConfig = Env.SaveManager.LoadFrom(backup);
        if (backupConfig != null) {
            Env.MacroConfig.OverwiteFrom(backupConfig);
            Env.SaveManager.Save(Env.MacroConfig);
            Refresh(Env.ConditionManager.CurrentConditions());
        }
    }

    /// Behaviours:
    ///
    /// - If a Macro becomes active: Bind it to it's linked slots
    /// - If a Macro becomes inactive: Unbind it from it's linked slots
    /// - If a previously linked slot becomes unlinked: Delete it
    private void Refresh(CurrentConditions conditions) {
        try {
            Env.MacroConfig.ActivateMacrosForConditions(conditions);

            var inactiveVanillaMacroLinks = CurrentVanillaLinks.ToList();
            foreach (var macro in Env.MacroConfig.ActiveMacros) {
                foreach (var (vanillaMacroLink, vanillaMacroOrNull) in macro.VanillaMacroLinkBinding()) {
                    Env.VanillaMacroManager.SetMacro(vanillaMacroLink, vanillaMacroOrNull);
                    inactiveVanillaMacroLinks.Remove(vanillaMacroLink);
                }
            }

            // We want to bind any inactive macro links to something so the button doesn't disappear off
            // the hotbar,
            foreach (var inactiveLink in inactiveVanillaMacroLinks) {
                Env.VanillaMacroManager.SetMacro(inactiveLink, VanillaMacro.Inactive);
            }

            var nextVanillaLinks = Env.MacroConfig.AllVanillaLinks();
            var removedVanillaLinks = CurrentVanillaLinks.Except(nextVanillaLinks);

            foreach (var link in removedVanillaLinks) {
                Env.VanillaMacroManager.DeleteMacro(link.Set, link.Slot);
            }

            CurrentVanillaLinks = nextVanillaLinks;
        } catch (Exception) {
            // If we fail for some reason, report it and then detach so we don't keep crashing things.
            // In an ideal world this would never happen
            Env.ChatGui.PrintError("Macro Mate: Failed to bind macros on condition change");
            Env.ChatGui.PrintError("Unbinding condition listener...");
            Env.ConditionManager.ConditionChange -= OnConditionChange;
            Env.MacroConfig.ConfigChange -= OnConfigChange;
            throw;
        }
    }

    private void OnConfigChange() {
        Env.SaveManager.Save(Env.MacroConfig);

        Env.PluginLog.Verbose("Config Change Detected - Refreshing Macro Bindings");
        Refresh(Env.ConditionManager.CurrentConditions());
    }

    private void OnConditionChange(CurrentConditions conditions) {
        Env.PluginLog.Verbose("Condition Change Detected - Refreshing Macro Bindings");
        Refresh(conditions);
    }
}
