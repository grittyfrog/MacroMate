using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Ipc;
using ImGuiNET;
using MacroMate.Extensions.Dalamud;

namespace MacroMate.Windows.Debug;

public class DebugWindow : Window {
    public static readonly string NAME = "Macro Mate Debug";

    private ICallGateSubscriber<bool> csIsAvailable;
    private ICallGateSubscriber<string, string, string?, uint?, bool> csCreateMacro;
    private ICallGateSubscriber<string, bool> csCreateGroup;

    public DebugWindow() : base(NAME, ImGuiWindowFlags.AlwaysAutoResize) {
        csIsAvailable = Env.PluginInterface.GetIpcSubscriber<bool>("MacroMate.IsAvailable");
        csCreateMacro = Env.PluginInterface.GetIpcSubscriber<string, string, string?, uint?, bool>("MacroMate.CreateOrUpdateMacro");
        csCreateGroup = Env.PluginInterface.GetIpcSubscriber<string, bool>("MacroMate.CreateGroup");
    }

    private string currentIpcFunction = "MacroMate.IsAvailable";
    private string[] ipcFunctions = new[] {
        "MacroMate.IsAvailable",
        "MacroMate.CreateOrUpdateMacro",
        "MacroMate.CreateGroup"
    };

    private string? lastIPCResult = null;
    private string? lastIPCError = null;
    public override void Draw() {
        ImGui.PushID("###debugwindow");

        ImGui.Text("IPC Debugging");
        ImGui.Separator();

        if (ImGui.BeginCombo("###ipc_combo", currentIpcFunction)) {
            foreach (var ipcFunction in ipcFunctions) {
                if (ImGui.Selectable(ipcFunction)) {
                    currentIpcFunction = ipcFunction;
                    lastIPCResult = null;
                    lastIPCError = null;
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine(); // Assume the first element is the call button
        switch (currentIpcFunction) {
            case "MacroMate.IsAvailable": DrawIpcIsAvailable(); break;
            case "MacroMate.CreateOrUpdateMacro": DrawIpcCreateMacro(); break;
            case "MacroMate.CreateGroup": DrawIpcCreateGroup(); break;
        }

        if (lastIPCResult != null) {
            ImGui.PushStyleColor(ImGuiCol.Text, Colors.ActiveGreen);
            ImGui.TextUnformatted($"result: {lastIPCResult}");
            ImGui.PopStyleColor();
        }
        if (lastIPCError != null) {
            ImGui.PushStyleColor(ImGuiCol.Text, Colors.ErrorRed);
            ImGui.TextUnformatted($"err: {lastIPCError}");
            ImGui.PopStyleColor();
        }

        ImGui.PopID();
    }

    private void DrawIpcIsAvailable() {
        if (ImGui.Button("Call IPC")) {
            try {
                var result = csIsAvailable.InvokeFunc();
                lastIPCResult = result.ToString();
            } catch (Exception ex) {
                lastIPCError = ex.InnerException?.Message ?? ex.Message;
            }
        }
    }

    private string createMacroName = "IPC Macro";
    private string createMacroLines = "/echo Hello World\n/echo This is IPC\n/echo Heliodor Carbuncle|65,152";
    private string createMacroParentPath = "";
    private string createMacroIconId = "";
    private void DrawIpcCreateMacro() {
        if (ImGui.Button("Call IPC")) {
            try {
                uint iconIdNumber = 66001;
                if (uint.TryParse(createMacroIconId, out var parsedIconId)) {
                    iconIdNumber = parsedIconId;
                }

                var result = csCreateMacro.InvokeFunc(
                    createMacroName,
                    createMacroLines,
                    createMacroParentPath,
                    iconIdNumber
                );
                lastIPCResult = result.ToString();
            } catch (Exception ex) {
                lastIPCError = ex.InnerException?.Message ?? ex.Message;
            }
        }

        bool edited = false;
        edited |= ImGui.InputText("Name", ref createMacroName, 255);
        edited |= ImGui.InputText("Parent Path", ref createMacroParentPath, 255);
        edited |= ImGui.InputText("Icon Id", ref createMacroIconId, 10);
        edited |= ImGui.InputTextMultiline(
            "###debugwindow/createorupdatemacrolines",
            ref createMacroLines,
            ushort.MaxValue,
            new Vector2(
                MathF.Min(ImGui.GetContentRegionAvail().X, 500f),
                ImGui.GetTextLineHeight() * 20
            )
        );

        if (edited) {
            lastIPCResult = null;
            lastIPCError = null;
        }
    }

    private string createGroupPath = "";
    private void DrawIpcCreateGroup() {
        if (ImGui.Button("Call IPC")) {
            try {
                var result = csCreateGroup.InvokeFunc(createGroupPath);
                lastIPCError = null;
                lastIPCResult = result.ToString();
            } catch (Exception ex) {
                lastIPCError = ex.InnerException?.Message ?? ex.Message;
                lastIPCResult = null;
            }
        }

        bool edited = false;
        edited |= ImGui.InputText("Path", ref createGroupPath, 1024);
    }
}
