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

            args.AddMenuItem(new MenuItem() {
                Name = "Import to Macro Mate",
                PrefixChar = 'M',
                OnClicked = (clickArgs) => OnImportToMacroMate(
                    clickArgs, agentMacro->SelectedMacroSet, agentMacro->SelectedMacroIndex
                )
            });
        }
    }

    private unsafe void OnImportToMacroMate(
        IMenuItemClickedArgs args,
        uint selectedMacroSet,
        uint selectedMacroIndex
    ) {
        var vanillaMacro = Env.VanillaMacroManager.GetMacro((VanillaMacroSet)selectedMacroSet, selectedMacroIndex);
        Env.PluginLog.Info($"{vanillaMacro}");

        var name = string.IsNullOrEmpty(vanillaMacro.Title) ? "Imported Macro" : vanillaMacro.Title;

        var importMacro = new MateNode.Macro {
            Name = name,
            IconId = vanillaMacro.IconId,
            Link = new MacroLink {
                Set = (VanillaMacroSet)selectedMacroSet,
                Slots = new() { selectedMacroIndex }
            },
            Lines = vanillaMacro.Lines.Value,
            AlwaysLinked = true,
        };

        Env.MacroConfig.MoveMacroIntoOrUpdate(
            importMacro,
            Env.MacroConfig.Root,
            (existing, replacement) => existing.Name == replacement.Name && existing.Link.Equals(replacement.Link)
        );
    }
}
