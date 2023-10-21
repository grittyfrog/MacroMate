using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace MacroMate.Windows;

public class PluginWindowManager : IDisposable {
    public MainWindow MainWindow { get; private set; } = new();
    public MacroWindow MacroWindow { get; private set; } = new();
    public IconPicker IconPicker { get; private set; } = new();
    public MacroLinkPicker MacroLinkPicker { get; private set; } = new();
    public BackupWindow BackupWindow { get; private set; } = new();
    public HelpWindow HelpWindow { get; private set; } = new();

    public PluginWindowManager() {
        Env.WindowSystem.AddWindow(MainWindow);
        Env.WindowSystem.AddWindow(MacroWindow);
        Env.WindowSystem.AddWindow(IconPicker);
        Env.WindowSystem.AddWindow(MacroLinkPicker);
        Env.WindowSystem.AddWindow(BackupWindow);
        Env.WindowSystem.AddWindow(HelpWindow);

        Env.PluginInterface.UiBuilder.Draw += DrawUI;
        Env.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    public void ShowOrFocus(Window window) {
        if (!window.IsOpen) {
            window.IsOpen = true;
        } else {
            ImGui.SetWindowFocus(window.WindowName);
        }
    }

    public void RightAlignedShowOrFocus(Window window) {
        if (!window.IsOpen) {
            var currentPos = ImGui.GetWindowPos();
            var currentSize = ImGui.GetWindowSize();

            window.IsOpen = true;
            window.Position = currentPos + new Vector2(currentSize.X, 0);
            window.PositionCondition = ImGuiCond.Appearing;
        } else {
            ImGui.SetWindowFocus(window.WindowName);
        }
    }

    public void Dispose() {
        Env.WindowSystem.RemoveAllWindows();
    }

    private void DrawUI() {
        Env.WindowSystem.Draw();
    }

    private void DrawConfigUI() {
        if (MainWindow != null) {
            MainWindow.IsOpen =  true;
        }
    }
}
