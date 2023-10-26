using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using FFXIVClientStructs.FFXIV.Component.GUI;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Extensions.Dalamud.Macros;

/// No longer used, kept as reference as an alterantive macro management system
public unsafe class VanillaMacroManager : IDisposable {
    private RaptureShellModule* raptureShellModule;
    private RaptureMacroModule* raptureMacroModule;
    private RaptureHotbarModule* raptureHotbarModule;

    private delegate void ReloadMacroSlotsDelegate(
        RaptureHotbarModule* raptureHotbarModule,
        byte macroSet,
        byte macroIndex
    );

    // TODO: Delete this and all related code when https://github.com/aers/FFXIVClientStructs/pull/660 lands
    [Signature("E8 ?? ?? ?? ?? 8B 83 ?? ?? ?? ?? 39 87", DetourName = nameof(ReloadDetour))]
    private Hook<ReloadMacroSlotsDelegate>? ReloadMacroSlotsHook { get; init; }

    // Hooks don't hook without a detour :(
    private void ReloadDetour(RaptureHotbarModule* raptureHotbarModule, byte macroSet, byte macroIndex) {
        ReloadMacroSlotsHook!.Original(raptureHotbarModule, macroSet, macroIndex);
    }

    public VanillaMacroManager() {
        Env.GameInteropProvider.InitializeFromAttributes(this);

        raptureShellModule = RaptureShellModule.Instance();
        raptureMacroModule = RaptureMacroModule.Instance();
        raptureHotbarModule = RaptureHotbarModule.Instance();

        if (ReloadMacroSlotsHook != null) {
            ReloadMacroSlotsHook.Enable();
        }
    }

    public void Dispose() {
        if (ReloadMacroSlotsHook != null) {
            ReloadMacroSlotsHook.Dispose();
        }
    }

    public VanillaMacro GetMacro(VanillaMacroSet macroSet, uint macroNumber) {
        if (macroNumber < 0 || macroNumber > 99) {
            throw new ArgumentException($"Invalid macroNumber: {macroNumber}");
        }

        var macro = raptureMacroModule->GetMacro((uint)macroSet, macroNumber);
        var lineCount = raptureMacroModule->GetLineCount(macro);
        var lazyLines = new Lazy<string>(() => {
            var lines = macro->LinesSpan
                .ToArray()
                .Take((int)lineCount)
                .Select(line => line.ToString());
            return string.Join("\n", lines);
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
        if (macroText.Count(c => c == '\n') > 15) {
            Env.ChatGui.PrintError($"Macro has too many lines (max 15)");
            return;
        }

        var macroPtr = IntPtr.Zero;
        try {
            macroPtr = Marshal.AllocHGlobal(Macro.size);
            var macro = new Macro(macroPtr, string.Empty, macroText.Split("\n"));
            Marshal.StructureToPtr(macro, macroPtr, false);

            raptureShellModule->ExecuteMacro((RaptureMacroModule.Macro*)Unsafe.AsRef(macroPtr));
            macro.Dispose();
        } catch (Exception e) {
            Env.PluginLog.Error($"Failed to execute macro {e}");
            Env.ChatGui.PrintError($"Failed to execute macro");
        }

        Marshal.FreeHGlobal(macroPtr);
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

        if (macroText.Count(c => c == '\n') > 15) {
            Env.ChatGui.PrintError($"Macro {macroSlot} has too many lines (max 15)");
            return;
        }

        var macro = raptureMacroModule->GetMacro((uint)macroSet, macroSlot);
        var macroNameUtf8 = new Utf8String();
        macroNameUtf8.SetString(vanillaMacro.Title.Truncate(15));
        macro->Name = macroNameUtf8;
        macro->IconId = vanillaMacro.IconId;
        macro->MacroIconRowId = vanillaMacro.IconRowId;

        var macroTextUtf8 = Utf8String.FromString(macroText);
        raptureMacroModule->ReplaceMacroLines(macro, macroTextUtf8);
        macroTextUtf8->Dtor();
        IMemorySpace.Free(macroTextUtf8);

        raptureMacroModule->SetSavePendingFlag(true, (uint)macroSet);
        raptureMacroModule->UserFileEvent.HasChanges = true;

        // Update the MacroAddon if it's open (since it doesn't auto-refresh)
        SetVanillaMacroUISlotIcon(macroSet, macroSlot, vanillaMacro.IconId);

        // TODO: Uncomment this when FFXIVClientStructs updates
        // raptureHotbarModule->ReloadMacroSlots((byte)macroSet, (byte)macroSlot);
        //
        if (ReloadMacroSlotsHook != null) {
            ReloadMacroSlotsHook.Original(raptureHotbarModule, (byte)macroSet, (byte)macroSlot);
        }
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

    /// When we change the Icon of a macro it doesn't actually refresh the Macro UI when it's open,
    /// so we need to write the UI values ourself.
    private void SetVanillaMacroUISlotIcon(VanillaMacroSet macroSet, uint macroSlot, uint iconId) {
        // Update the MacroAddon if it's open (since it doesn't auto-refresh)
        var macroAddonRaw = Env.GameGui.GetAddonByName("Macro");
        if (macroAddonRaw == nint.Zero) { return; }
        var macroAddon = (AtkUnitBase*)macroAddonRaw;

        var macroInterfaceRaw = Env.GameGui.FindAgentInterface(macroAddon);
        if (macroInterfaceRaw == nint.Zero) { return; }
        var macroInterface = (AgentInterface*)macroInterfaceRaw;

        // This prevents a crash when editing the icon prior to opening the
        // interface for the first time and writing to a previously deleted slot.
        if (!macroInterface->IsAgentActive()) { return; }

        // We only want to update the screen if it's visible, since otherwise we'll be writing
        // shared icons into individual and vice-versa.
        var addonMacroSet = (VanillaMacroSet)macroAddon->AtkValues[05].Int; // Individual / Shared
        if (macroSet == addonMacroSet) {
            // This sets the active selection of the macro window. It's a bit of a hack but
            // I couldn't see any other way to "target" the slot.
            macroAddon->AtkValues[04].Int = (int)macroSlot;

            // This sets the icon of the active selection
            macroAddon->AtkValues[12].Int = (int)iconId;

            macroAddon->OnRefresh(macroAddon->AtkValuesCount, macroAddon->AtkValues);
        }
    }
}
