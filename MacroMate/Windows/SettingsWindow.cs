using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.Imgui;

namespace MacroMate.Windows;

public class SettingsWindow : Window {
    public static readonly string NAME = "Settings";

    public SettingsWindow() : base(NAME, ImGuiWindowFlags.AlwaysAutoResize) {}

    public override void Draw() {
        if (ImGui.BeginTable("settingswindow/layout_table", 2, ImGuiTableFlags.SizingStretchProp)) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("General");
            ImGui.Separator();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.NewLine();
            ImGui.Separator();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            bool linkPlaceHolderIconHovered = false;
            bool linkPlaceHolderIconClicked = false;
            bool linkPlaceHolderIconRightClicked = false;
            if (ImGui.Selectable("Set Link Placeholder Icon")) {
                linkPlaceHolderIconClicked = true;
            }
            linkPlaceHolderIconHovered |= ImGui.IsItemHovered();
            linkPlaceHolderIconRightClicked |= ImGui.IsItemClicked(ImGuiMouseButton.Right);

            ImGui.TableNextColumn();
            var macroIcon = Env.TextureProvider.GetMacroIcon(Env.MacroConfig.LinkPlaceholderIconId).GetWrapOrEmpty();
            if (macroIcon != null) {
                ImGui.Image(macroIcon.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight()) * 1.3f);
                ImGui.SameLine();
            }
            linkPlaceHolderIconHovered |= ImGui.IsItemHovered();
            linkPlaceHolderIconClicked |= ImGui.IsItemClicked(ImGuiMouseButton.Left);
            linkPlaceHolderIconRightClicked |= ImGui.IsItemClicked(ImGuiMouseButton.Right);

            if (linkPlaceHolderIconHovered) {
                ImGui.SetTooltip("Select the icon used for macro slots that are managed by Macro Mate but currently unlinked (right click to reset to default)");
            }
            if (linkPlaceHolderIconClicked) {
                Env.PluginWindowManager.IconPicker.Open(Env.MacroConfig.LinkPlaceholderIconId, selectedIconId => {
                    Env.MacroConfig.LinkPlaceholderIconId = selectedIconId;
                });
            }
            if (linkPlaceHolderIconRightClicked) {
                Env.MacroConfig.LinkPlaceholderIconId = VanillaMacro.InactiveIconId;
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            bool showVanillaMacroContextMenusHovered = false;
            ImGui.TextUnformatted("Show Vanilla Macro Context Menus");
            showVanillaMacroContextMenusHovered |= ImGui.IsItemHovered();
            ImGui.TableNextColumn();
            var showVanillaMacroContextMenus = Env.MacroConfig.ShowVanillaMacroContextMenus;
            if (ImGui.Checkbox("###settingswindow/show_vanilla_macro_context_menus", ref showVanillaMacroContextMenus)) {
                Env.MacroConfig.ShowVanillaMacroContextMenus = showVanillaMacroContextMenus;
            }
            showVanillaMacroContextMenusHovered |= ImGui.IsItemHovered();
            if (showVanillaMacroContextMenusHovered) {
                ImGui.SetTooltip("When disabled no Macro Mate context menu actions will be shown in the vanilla macro UI");
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            bool macroChainAvailable = Env.PluginInterface.MacroChainPluginIsLoaded();
            bool useMacroChainByDefaultHovered = false;
            if (!macroChainAvailable) { ImGui.BeginDisabled(); }
            ImGui.TextUnformatted("Use Macro Chain by Default");
            useMacroChainByDefaultHovered |= ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);

            ImGui.TableNextColumn();
            var useMacroChainByDefault = Env.MacroConfig.UseMacroChainByDefault;
            if (ImGui.Checkbox("###settingswindow/use_macro_chain_by_default", ref useMacroChainByDefault)) {
                Env.MacroConfig.UseMacroChainByDefault = useMacroChainByDefault;
            }
            if (!macroChainAvailable) { ImGui.EndDisabled(); }
            useMacroChainByDefaultHovered |= ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
            if (useMacroChainByDefaultHovered) {
                if (macroChainAvailable) {
                    ImGui.SetTooltip("If enabled, all newly created macros will enable the 'Use Macro Chain' setting");
                } else {
                    ImGui.SetTooltip("This setting is only availalbe if Macro Chain is installed.");
                }
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.NewLine();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Subscriptions");
            ImGui.Separator();

            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            ImGui.NewLine();
            ImGui.Separator();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            bool enableSubscriptionAutoCheckForUpdatesHovered = false;
            ImGui.TextUnformatted("Automatically Check for Subscription Updates");
            enableSubscriptionAutoCheckForUpdatesHovered |= ImGui.IsItemHovered();

            ImGui.TableNextColumn();
            var enableSubscriptionAutoCheckForUpdates = Env.MacroConfig.EnableSubscriptionAutoCheckForUpdates;
            if (ImGui.Checkbox("###settingswindow/automatically_check_for_subscription_updates", ref enableSubscriptionAutoCheckForUpdates)) {
                Env.MacroConfig.EnableSubscriptionAutoCheckForUpdates = enableSubscriptionAutoCheckForUpdates;
            }
            enableSubscriptionAutoCheckForUpdatesHovered |= ImGui.IsItemHovered();

            if (enableSubscriptionAutoCheckForUpdatesHovered) {
                ImGui.SetTooltip("When disabled Macro Mate will not automatically check for subscription updates. You will need to manually use the 'Check for Updates' action");
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.AlignTextToFramePadding();
            if (!Env.MacroConfig.EnableSubscriptionAutoCheckForUpdates) { ImGui.BeginDisabled(); }
            bool automaticallyCheckForUpdatesInternalHovered = false;
            ImGui.TextUnformatted("Automatic Check for Updates Interval (Minutes)");
            automaticallyCheckForUpdatesInternalHovered |= ImGui.IsItemHovered();

            ImGui.TableNextColumn();
            int minutesBetweenSubscriptionAutoCheckForUpdates = Env.MacroConfig.MinutesBetweenSubscriptionAutoCheckForUpdates;
            ImGui.SetNextItemWidth(100);
            if (ImGuiExt.InputTextInt("###settingswindow/minutes_between_subscription_auto_check_for_updates", ref minutesBetweenSubscriptionAutoCheckForUpdates)) {
                Env.MacroConfig.MinutesBetweenSubscriptionAutoCheckForUpdates =
                    Math.Clamp(minutesBetweenSubscriptionAutoCheckForUpdates, 1, 10080);
            }
            automaticallyCheckForUpdatesInternalHovered |= ImGui.IsItemHovered();

            if (automaticallyCheckForUpdatesInternalHovered) {
                ImGui.SetTooltip("The number of minutes to wait between automatically checking for updates");
            }
            if (!Env.MacroConfig.EnableSubscriptionAutoCheckForUpdates) { ImGui.EndDisabled(); }

            ImGui.EndTable();
        }
    }
}
