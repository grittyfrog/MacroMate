
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using MacroMate.Extensions.Imgui;

namespace MacroMate.Windows;

public class BackupWindow : Window {
    public static readonly string NAME = "Backups";

    public BackupWindow() : base(
        NAME,
        ImGuiWindowFlags.MenuBar
    ) {}

    public void ShowOrFocus() {
        Env.PluginWindowManager.ShowOrFocus(this);
    }

    public override void Draw() {
        DrawMenuBar();
        DrawBackups();
    }

    private void DrawMenuBar() {
        if (ImGui.BeginMenuBar()) {
            if (ConditionalMenuItem("Create Backup", Env.SaveManager.CanBackup(), disabledTooltip: "No data to backup")) {
                Env.SaveManager.SaveBackup();
            }

            if (ConditionalMenuItem(
                "Open Backup Folder",
                Env.SaveManager.MacroBackupFolder.Exists,
                disabledTooltip: "Backup folder doesn't exist yet!"
            )) {
                Process.Start(startInfo: new ProcessStartInfo {
                    FileName = Env.SaveManager.MacroBackupFolder.FullName,
                    UseShellExecute = true
                });
            }

            if (ConditionalMenuItem(
                "Reload Config File",
                Env.MacroMate.CanLoadConfig(),
                "Reload the current MacroMate.xml config file",
                "Config file doesn't exist yet!"
            )) {
                Env.MacroMate.LoadConfig();
                Env.ChatGui.Print("Macro Mate config reloaded");
            }

            ImGui.EndMenuBar();
        }
    }

    private void DrawBackups() {
        if (ImGui.BeginTable("backup_window_backups_table", 4, ImGuiTableFlags.BordersH)) {
            ImGui.TableSetupColumn("FileName", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("ModifiedDate", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("LoadButton", ImGuiTableColumnFlags.WidthFixed);
            ImGui.TableSetupColumn("DeleteButton", ImGuiTableColumnFlags.WidthFixed);

            var backups = Env.SaveManager.ListBackups();
            foreach (var backup in backups) {
                ImGui.PushID(backup.FullName);

                var loadPopup = DrawLoadPopupModal(backup);

                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(backup.Name);

                ImGui.TableNextColumn();
                var localBackupTime = backup.LastAccessTime.ToLocalTime();
                var date = localBackupTime.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
                var time = localBackupTime.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern);
                ImGui.Text($"{date} {time}");

                ImGui.TableNextColumn();
                if (ImGui.Button("Load")) {
                    ImGui.OpenPopup(loadPopup);
                }

                ImGui.TableNextColumn();
                ImGui.Button("Delete");
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip($"Double right click this button to delete {backup.Name}");

                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right)) {
                        backup.Delete();
                    }
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }

    }

    private uint DrawLoadPopupModal(FileInfo backup) {
        var loadPopupName = $"Load###backups_window/load_popup/{backup.FullName}";
        var loadPopupId = ImGui.GetID(loadPopupName);

        if (ImGuiExt.BeginPopupModal(loadPopupName, ImGuiWindowFlags.AlwaysAutoResize)) {
            ImGui.Text($"Are you sure you want to load {backup.Name}?");
            ImGui.Text("This will overwrite the current configuration");
            if (ImGui.Button("Yes")) {
                Env.MacroMate.LoadBackup(backup);
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No")) {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        return loadPopupId;
    }

    /// <summary>
    /// Draw a menu item that is only enabled conditionally
    /// </summary>
    private bool ConditionalMenuItem(
        string label,
        bool enabled,
        string enabledTooltip = "",
        string disabledTooltip = ""
    ) {
        if (!enabled) { ImGui.BeginDisabled(); }

        bool menuItemClicked = ImGui.MenuItem(label);

        if (!enabled) { ImGui.EndDisabled(); }

        var tooltip = enabled ? enabledTooltip : disabledTooltip;
        if (tooltip != "") {
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                ImGui.SetTooltip(tooltip);
            }
        }

        return enabled && menuItemClicked;
    }
}
