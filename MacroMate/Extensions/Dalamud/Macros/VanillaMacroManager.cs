using System;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MacroMate.Extensions.Dalamud.Str;
using MacroMate.Extensions.Dotnet;
using MacroMate.Extensions.FFXIVClientStructs;

namespace MacroMate.Extensions.Dalamud.Macros;

public unsafe class VanillaMacroManager : IDisposable {
    private RaptureShellModule* raptureShellModule;
    private RaptureMacroModule* raptureMacroModule;
    private RaptureHotbarModule* raptureHotbarModule;

    private bool macroAddonIsSetup = false;

    public VanillaMacroManager() {
        raptureShellModule = RaptureShellModule.Instance();
        raptureMacroModule = RaptureMacroModule.Instance();
        raptureHotbarModule = RaptureHotbarModule.Instance();

        Env.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "Macro", OnMacroAddonPostSetup);
        Env.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "Macro", OnMacroAddonPreFinalise);
    }

    public void Dispose() {
        Env.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "Macro", OnMacroAddonPostSetup);
        Env.AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, "Macro", OnMacroAddonPreFinalise);
    }

    public VanillaMacro GetMacro(VanillaMacroSet macroSet, uint macroNumber) {
        if (macroNumber < 0 || macroNumber > 99) {
            throw new ArgumentException($"Invalid macroNumber: {macroNumber}");
        }

        var macro = raptureMacroModule->GetMacro((uint)macroSet, macroNumber);
        var lineCount = raptureMacroModule->GetLineCount(macro);
        var lazyLines = new Lazy<SeString>(() => {
            return macro->LinesSpan
                .ToArray()
                .Take((int)lineCount)
                .Select(line => SeString.Parse(line))
                .Let(lines => SeStringEx.JoinFromLines(lines));
        });

        return new VanillaMacro(
            IconId: macro->IconId,
            Title: macro->Name.ToString(),
            LineCount: lineCount,
            Lines: lazyLines,
            IconRowId: macro->MacroIconRowId
        );
    }

    public void ExecuteMacro(VanillaMacro vanillaMacro) {
        var macroText = vanillaMacro.Lines.Value;
        if (macroText.CountNewlines() > 15) {
            Env.ChatGui.PrintError($"Macro has too many lines (max 15)");
            return;
        }

        // We don't have a constructor to call so we need to initialize the memory ourselves
        //
        // Without this we end up corrupting the game memory, since the macro lines are copied
        // into another buffer by the in-game `ExecuteMacro` function, and it assumes that the macro
        // object is initialized.
        var macro = new RaptureMacroModule.Macro();
        macro.Name.Ctor();
        foreach (ref var line in macro.LinesSpan) {
            line.Ctor();
        }

        try {
            var lines = macroText.SplitIntoLines()
                .TakeWhile(line => line.Payloads.Count > 0)
                .Select(line => line.Encode())
                .ToArray();

            foreach (var (line, index) in macroText.SplitIntoLines().WithIndex()) {
                if (line.Payloads.Count == 0 || line.Encode().Length == 0) {
                    macro.LinesSpan[index].Clear();
                    continue;
                }

                var encoded = line.Encode();
                if (encoded.Length == 0 || encoded.Any(c => c == 0)) {
                    macro.LinesSpan[index].Clear();
                    continue;
                }

                fixed (byte* encodedPtr = encoded) {
                    macro.LinesSpan[index].SetString(encodedPtr);
                }
            }

            raptureShellModule->ExecuteMacro(&macro);
        } catch (Exception e) {
            Env.PluginLog.Error($"Failed to execute macro {e}");
            Env.ChatGui.PrintError($"Failed to execute macro");
        } finally {
            foreach (ref var line in macro.LinesSpan) {
                line.Dtor();
            }
        }
    }

    public void SetMacro(VanillaMacroLink link, VanillaMacro vanillaMacro) {
        SetMacro(link.Set, link.Slot, vanillaMacro);
    }

    public void SetMacro(
        VanillaMacroSet macroSet,
        uint macroSlot,
        VanillaMacro vanillaMacro
    ) {
        if (macroSlot < 0 || macroSlot > 99) {
            Env.ChatGui.PrintError($"Invalid macro slot: {macroSlot}");
            return;
        }

        var macroText = vanillaMacro.Lines.Value;

        if (macroText.CountNewlines() > 15) {
            Env.ChatGui.PrintError($"Macro {macroSlot} has too many lines (max 15)");
            return;
        }

        var macro = raptureMacroModule->GetMacro((uint)macroSet, macroSlot);
        macro->Name.SetString(vanillaMacro.Title.Truncate(15));

        bool iconIdChanged = macro->IconId != vanillaMacro.IconId;
        macro->SetIcon(vanillaMacro.IconId);

        // We don't want to use NewLinePayload since it encodes to an extra newline, instead we just encode everything to '\r'.
        // We also don't use macro-LineSpan[index] = ... here since it causes memory access errors
        var macroLinePayload = macroText.ReplaceNewlinesWithCR();
        var macroLinePayloadUtf8 = Utf8String.FromSequence(macroLinePayload.Encode());
        raptureMacroModule->ReplaceMacroLines(macro, macroLinePayloadUtf8);
        macroLinePayloadUtf8->Dtor();
        IMemorySpace.Free(macroLinePayloadUtf8);

        // Update the MacroAddon if it's open (since it doesn't auto-refresh)
        if (iconIdChanged) {
            SetVanillaMacroUISlotIcon(macroSet, macroSlot, vanillaMacro.IconId);
        }

        raptureHotbarModule->ReloadMacroSlots((byte)macroSet, (byte)macroSlot);
    }

    public void DeleteMacro(VanillaMacroSet macroSet, uint macroSlot) {
        if (macroSlot < 0 || macroSlot > 99) {
            Env.ChatGui.PrintError($"Invalid macro slot: {macroSlot}");
            return;
        }

        var deletedMacro = new VanillaMacro(
            IconId: 0,
            Title: "",
            LineCount: 0,
            Lines: new(() => ""),
            IconRowId: 0
        );
        SetMacro(macroSet, macroSlot, deletedMacro);
    }

    public void EditMacroInUI(VanillaMacroSet macroSet, uint macroSlot) {
        var agentMacro = XIVCS.GetAgent<AgentMacro>();
        if (agentMacro == null) { return; }

        Env.XIVCSSignatures.AgentMacroOpenMacro!(agentMacro, (uint)macroSet, macroSlot);
    }

    /// When we change the Icon of a macro it doesn't actually refresh the Macro UI when it's open,
    /// so we need to write the UI values ourself.
    private void SetVanillaMacroUISlotIcon(VanillaMacroSet macroSet, uint macroSlot, uint iconId) {
        var addonMacro = GetAddonMacro();
        if (addonMacro == null) { return; }

        // We only want to update the screen if it's visible, since otherwise we'll be writing
        // shared icons into individual and vice-versa.
        var addonMacroSet = (VanillaMacroSet)addonMacro->AtkValues[05].Int; // Individual / Shared
        if (macroSet == addonMacroSet) {
            // This sets the active selection of the macro window. It's a bit of a hack but
            // I couldn't see any other way to "target" the slot.
            addonMacro->AtkValues[04].Int = (int)macroSlot;

            // This sets the icon of the active selection
            addonMacro->AtkValues[12].Int = (int)iconId;

            addonMacro->OnRefresh(addonMacro->AtkValuesCount, addonMacro->AtkValues);
        }
    }

    private void OnMacroAddonPostSetup(AddonEvent type, AddonArgs args) {
        macroAddonIsSetup = true;
    }

    private void OnMacroAddonPreFinalise(AddonEvent type, AddonArgs args) {
        macroAddonIsSetup = false;
    }

    private AtkUnitBase* GetAddonMacro() {
        var macroAddonRaw = Env.GameGui.GetAddonByName("Macro");
        if (macroAddonRaw == nint.Zero) { return null; }
        var macroAddon = (AtkUnitBase*)macroAddonRaw;

        // We need to make sure the addon is fully initialised (i.e. Setup has finished) before
        // calling OnRefresh, otherwise things crash.
        if (macroAddon->RootNode == null) { return null; }
        if (macroAddon->RootNode->ChildNode == null) { return null; }

        // Just for double extra safety we also make sure we've seen a full setup event.
        //
        // This should be redundant given the above checks on RootNode and ChildNode, but lets check it
        // anyway to be sure.
        if (!macroAddonIsSetup) { return null; }

        return macroAddon;
    }
}
