using Dalamud.Game.Gui.ContextMenu;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.FFXIVClientStructs;
using MacroMate.MacroTree;

namespace MacroMate.ContextMenu;

public class ContextMenuManager {
    public ContextMenuManager() {
        Env.ContextMenu.OnMenuOpened += OnContextMenuOpened;
    }

    private unsafe void OnContextMenuOpened(IMenuOpenedArgs args) {
        if (args.MenuType != ContextMenuType.Default) { return; }
        if (args.Target is not MenuTargetDefault mtd) { return; }
        if (string.IsNullOrWhiteSpace(args.AddonName)) { return; }
        if (mtd.TargetObjectId != 0xE0000000) { return; }

        if (args.AddonName == "Macro") {
            if (!Env.MacroConfig.ShowVanillaMacroContextMenus) { return; }

            // Don't use args.AgentPtr here, it seems to point to something that isn't AgentMacro
            var agentMacro = XIVCS.GetAgent<AgentMacro>();
            if (agentMacro == null) { return; }

            var raptureMacroModule = RaptureMacroModule.Instance();
            var macro = raptureMacroModule->GetMacro(
                agentMacro->SelectedMacroSet,
                agentMacro->SelectedMacroIndex
            );

            var macroLineCount = raptureMacroModule->GetLineCount(macro);

            // Only add the context menu to macros that have content
            //
            // Note: We don't use `Macro->IsEmpty()` because the behaviour seems to
            // actually be `IsNotEmpty()` and I don't feel like fixing up Ghidra to
            // find the right signature.
            if (macroLineCount == 0) { return; }

            // If we've right clicked on a macro that is already linked from Macro Mate then
            // we are updating the macro, rather then importing it. The actual function is the
            // same but we should change the menu text
            var activeMacro = Env.MacroConfig.ActiveMacroLinkedTo(
                (VanillaMacroSet)agentMacro->SelectedMacroSet,
                agentMacro->SelectedMacroIndex
            );
            if (activeMacro != null) {
                args.AddMenuItem(new MenuItem() {
                    Name = "Open in Macro Mate",
                    PrefixChar = 'M',
                    OnClicked = (clickArgs) => OnOpenInMacroMate(clickArgs, activeMacro)
                });

                args.AddMenuItem(new MenuItem() {
                    Name = "Update in Macro Mate",
                    PrefixChar = 'M',
                    OnClicked = (clickArgs) => OnUpdateInMacroMate(
                        clickArgs,
                        activeMacro,
                        (VanillaMacroSet)agentMacro->SelectedMacroSet,
                        agentMacro->SelectedMacroIndex
                    )
                });
            } else {
                args.AddMenuItem(new MenuItem() {
                    Name = "Import to Macro Mate",
                    PrefixChar = 'M',
                    OnClicked = (clickArgs) => OnImportToMacroMate(
                        clickArgs,
                        (VanillaMacroSet)agentMacro->SelectedMacroSet,
                        agentMacro->SelectedMacroIndex
                    )
                });
            }

            var importTitle = activeMacro == null ? "Import to Macro Mate" : "Update in Macro Mate";

        }
    }

    private void OnOpenInMacroMate(IMenuItemClickedArgs args, MateNode.Macro activeMacro) {
        Env.PluginWindowManager.MacroWindow.ShowOrFocus(activeMacro);
    }

    private void OnUpdateInMacroMate(
        IMenuItemClickedArgs args,
        MateNode.Macro activeMacro,
        VanillaMacroSet selectedMacroSet,
        uint selectedMacroSlot
    ) {
        Env.MacroConfig.UpdateFromXIV(activeMacro, selectedMacroSet, selectedMacroSlot);
    }

    private unsafe void OnImportToMacroMate(
        IMenuItemClickedArgs args,
        VanillaMacroSet selectedMacroSet,
        uint selectedMacroSlot
    ) {
        Env.MacroConfig.ImportFromXIV(Env.MacroConfig.Root, selectedMacroSet, selectedMacroSlot);
    }
}
