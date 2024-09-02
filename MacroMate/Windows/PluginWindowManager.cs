using System;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using MacroMate.Extensions.Dalamud.Interface.ImGuiIconPicker;
using MacroMate.Windows.Debug;

namespace MacroMate.Windows;

public class PluginWindowManager : IDisposable {
    public MainWindow MainWindow { get; private set; } = new();
    public SettingsWindow SettingsWindow { get; private set; } = new();
    public MacroWindow MacroWindow { get; private set; } = new();
    public IconPickerDialog IconPicker { get; private set; } = new();
    public MacroLinkPicker MacroLinkPicker { get; private set; } = new();
    public BackupWindow BackupWindow { get; private set; } = new();
    public SubscriptionStatusWindow SubscriptionStatusWindow { get; private set; } = new();
    public HelpWindow HelpWindow { get; private set; } = new();
    public DebugWindow DebugWindow { get; private set; } = new();

    public PluginWindowManager() {
        Env.WindowSystem.AddWindow(MainWindow);
        Env.WindowSystem.AddWindow(SettingsWindow);
        Env.WindowSystem.AddWindow(MacroWindow);
        Env.WindowSystem.AddWindow(IconPicker);
        Env.WindowSystem.AddWindow(MacroLinkPicker);
        Env.WindowSystem.AddWindow(BackupWindow);
        Env.WindowSystem.AddWindow(SubscriptionStatusWindow);
        Env.WindowSystem.AddWindow(HelpWindow);
        Env.WindowSystem.AddWindow(DebugWindow);

        Env.PluginInterface.UiBuilder.Draw += DrawUI;
        Env.PluginInterface.UiBuilder.OpenMainUi += DrawMainUI;
        Env.PluginInterface.UiBuilder.OpenConfigUi += DrawSettingsUI;
    }

    public void ShowOrFocus(Window window) {
        if (!window.IsOpen) {
            window.IsOpen = true;
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

    private void DrawMainUI() {
        if (MainWindow != null) {
            MainWindow.IsOpen =  true;
        }
    }

    private void DrawSettingsUI() {
        if (SettingsWindow != null) {
            SettingsWindow.IsOpen =  true;
        }
    }
}
