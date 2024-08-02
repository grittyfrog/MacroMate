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

            var importTitle = activeMacro == null ? "Import to Macro Mate" : "Update in Macro Mate";

            args.AddMenuItem(new MenuItem() {
                Name = importTitle,
                PrefixChar = 'M',
                OnClicked = (clickArgs) => OnImportToMacroMate(
                    clickArgs,
                    activeMacro,
                    (VanillaMacroSet)agentMacro->SelectedMacroSet,
                    agentMacro->SelectedMacroIndex
                )
            });
        }
    }

    private unsafe void OnImportToMacroMate(
        IMenuItemClickedArgs args,
        MateNode.Macro? activeMacro,
        VanillaMacroSet selectedMacroSet,
        uint selectedMacroSlot
    ) {
        var vanillaMacro = Env.VanillaMacroManager.GetMacro(selectedMacroSet, selectedMacroSlot);
        var name = string.IsNullOrEmpty(vanillaMacro.Title) ? "Imported Macro" : vanillaMacro.Title;

        var importMacro = new MateNode.Macro {
            Name = name,
            IconId = vanillaMacro.IconId,
            Link = new MacroLink {
                Set = selectedMacroSet,
                Slots = new() { selectedMacroSlot }
            },
            Lines = vanillaMacro.Lines.Value,
            AlwaysLinked = true,
        };

        // If we've got an active macro then we should "move" into it which will cause
        // us to update.
        //
        // Otherwise just use root, where we might import or update.
        var parent = activeMacro?.Parent ?? Env.MacroConfig.Root;

        Env.MacroConfig.MoveMacroIntoOrUpdate(
            importMacro,
            parent,
            (existing, replacement) => existing.Name == replacement.Name && existing.Link.Equals(replacement.Link)
        );
    }
}
