using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Utility;
using ImGuiNET;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dalamud.Icons;
using MacroMate.Extensions.Dalamud.Texture;
using MacroMate.MacroTree;

namespace MacroMate.Windows;

/// Class to pick the icon for a macro.
///
/// Inspirations:
/// - QoLBar's IconBrowser: https://github.com/UnknownX7/QoLBar/blob/f80d64ab064e4e7574a42b2efc678beebdcb1af9/UI/IconBrowserUI.cs
/// - Dalamud's IconBrowserWidget: https://github.com/goatcorp/Dalamud/blob/deef16cdd742ca9faa403e388602795e9d3b54e9/Dalamud/Interface/Internal/Windows/Data/Widgets/IconBrowserWidget.cs#L16
public class IconPicker : EventWindow<uint>, IDisposable {
    private record struct IconTab(
        string Name,
        List<IconGroup> IconGroups
    ) {}

    public static readonly string NAME = "Icon Picker";

    private uint? CurrentIconId { get; set; }

    private TextureCache TextureCache = new(Env.TextureProvider);
    private List<IconTab> iconTabs = new();

    private NamedIconIndex namedIconIndex = new();

    private string searchText = "";
    private List<IconInfo> searchedIconInfo = new();

    private bool showIconNames = false;

    public IconPicker() : base(NAME) {
        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        AddIconTab(
            " ★ ",
            new IconGroup(1..100, "System"),
            new IconGroup(62_000..62_600, "Class/Job"),
            new IconGroup(62_800..62_900, "Gearsets"),
            new IconGroup(66_000..66_400, "Macros"),
            new IconGroup(90_000..100_000, "FC Crests/Symbols"),
            new IconGroup(114_000..114_100, "New Game+")
        );

        AddIconTab(
            "Misc",
            new IconGroup(60_000..61_000, "UI"),
            new IconGroup(61_200..61_250, "Markers"),
            new IconGroup(61_290..61_390, "Markers 2"),
            new IconGroup(61_390..62_000, "UI 2"),
            new IconGroup(62_600..62_620, "HQ FC Banners"),
            new IconGroup(63_900..64_000, "Map Markers"),
            new IconGroup(64_500..64_600, "Stamps"),
            new IconGroup(65_000..65_900, "Currencies"),
            new IconGroup(76_300..78_000, "Group Pose"),
            new IconGroup(180_000..180_060, "Stamps/Chocobo Racing")
        );

        AddIconTab(
            "Misc 2",
            new IconGroup(62_900..63_200, "Achievements/Hunting Log"),
            new IconGroup(65_900..66_000, "Fishing"),
            new IconGroup(66_400..66_500, "Tags"),
            new IconGroup(67_000..68_000, "Fashion Log"),
            new IconGroup(71_000..71_500, "Quests"),
            new IconGroup(72_000..72_500, "BLU UI"),
            new IconGroup(72_500..76_000, "Bozja UI"),
            new IconGroup(76_000..76_200, "Mahjong"),
            new IconGroup(80_000..80_200, "Quest Log"),
            new IconGroup(80_730..81_000, "Relic Log"),
            new IconGroup(83_000..84_000, "FC Ranks")
        );

        AddIconTab(
            "Actions",
            new IconGroup(100..4_000, "Classes/Jobs"),
            new IconGroup(5_100..8_000, "Traits"),
            new IconGroup(8_000..9_000, "Fashion"),
            new IconGroup(9_000..10_000, "PvP"),
            new IconGroup(61_100..61_200, "Event"),
            new IconGroup(61_250..61_290, "Duties/Trials"),
            new IconGroup(64_000..64_200, "Emotes"),
            new IconGroup(64_200..64_325, "FC"),
            new IconGroup(64_325..64_500, "Emotes 2"),
            new IconGroup(64_600..64_800, "Eureka"),
            new IconGroup(64_800..65_000, "NPC"),
            new IconGroup(70_000..70_200, "Chocobo Racing")
        );

        AddIconTab(
            "Mounts & Minions",
            new IconGroup(4_000..4_400, "Mounts"),
            new IconGroup(4_400..5_100, "Minions"),
            new IconGroup(59_000..59_400, "Mounts... again?"),
            new IconGroup(59_400..60_000, "Minion Items"),
            new IconGroup(68_000..68_400, "Mounts Log"),
            new IconGroup(68_400..69_000, "Minions Log")
        );

        AddIconTab(
            "Items",
            new IconGroup(20_000..30_000, "General"),
            new IconGroup(50_000..54_400, "Housing"),
            new IconGroup(58_000..59_000, "Fashion")
        );

        AddIconTab(
            "Equipment",
            new IconGroup(30_000..50_000, "Equipment"),
            new IconGroup(54_400..58_000, "Special Equipment")
        );

        AddIconTab(
           "Aesthetics",
            new IconGroup(130_000..142_000, "Aesthetics")
        );

        AddIconTab(
            "Statuses",
            new IconGroup(10_000..20_000, "Statuses")
        );

        AddIconTab(
            "Garbage",
            new IconGroup(61_000..61_100, "Splash Logos"),
            new IconGroup(62_620..62_800, "World Map"),
            new IconGroup(63_200..63_900, "Zone Maps"),
            new IconGroup(66_500..67_000, "Gardening Log"),
            new IconGroup(69_000..70_000, "Mount/Minion Footprints"),
            new IconGroup(70_200..71_000, "DoH/DoL Logs"),
            new IconGroup(76_200..76_300, "Fan Festival"),
            new IconGroup(78_000..80_000, "Fishing Log"),
            new IconGroup(80_200..80_730, "Notebooks"),
            new IconGroup(81_000..82_060, "Notebooks 2"),
            new IconGroup(84_000..85_000, "Hunts"),
            new IconGroup(85_000..90_000, "UI 3"),
            new IconGroup(150_000..170_000, "Tutorials")
            //new IconGroup(170_000..180_000, "Placeholder"); // TODO: 170k - 180k are blank placeholder files, check if they get used in EW
        );

        AddIconTab(
            "Spoilers",
            new IconGroup(82_100..83_000, "Triple Triad"), // Out of order because people might want to use these
            new IconGroup(82_060..82_100, "Trusts"),
            new IconGroup(120_000..130_000, "Popup Texts"),
            new IconGroup(142_000..150_000, "Japanese Popup Texts"),
            new IconGroup(180_060..180_100, "Trusts Names"),
            new IconGroup(181_000..181_500, "Boss Titles"),
            new IconGroup(181_500..200_000, "Placeholder")
        );

        AddIconTab(
            "Spoilers 2",
            new IconGroup(71_500..72_000, "Credits"),
            new IconGroup(100_000..114_000, "Quest Images"),
            new IconGroup(114_100..120_000, "New Game+")
        );
    }

    public override void OnOpen() {
        namedIconIndex.StartIndexing(() => {
            RefreshSearch();
        });
    }

    private void RefreshSearch() {
        searchedIconInfo = namedIconIndex.NameSearch(searchText);
    }

    public override void OnClose() {
        base.OnClose();
        TextureCache.Clear();
    }

    private void AddIconTab(string name, params IconGroup[] iconGroups) {
        iconTabs.Add(new IconTab(name, iconGroups.ToList()));
    }

    public void ShowOrFocus(string scopeName, uint? initialIcon = null) {
        CurrentIconId = initialIcon;
        CurrentEventScope = scopeName;
        Env.PluginWindowManager.ShowOrFocus(this);
    }

    public void Dispose() {
        TextureCache.Dispose();
    }

    public override void Draw() {
        var allIconTabs = iconTabs.ToList();

        var activeIconsTab = ActiveIconsTab();
        allIconTabs.Insert(0, activeIconsTab);

        float iconSize = 48 * ImGuiHelpers.GlobalScale;
        var columns = (int)((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X) / (iconSize + ImGui.GetStyle().ItemSpacing.X));
        if (ImGui.BeginTabBar("Icon Tabs", ImGuiTabBarFlags.NoTooltip)) {
            DrawSearchTab(iconSize, columns);

            foreach (var iconTab in allIconTabs) {
                DrawIconTab(iconTab, iconSize, columns);
            }
            ImGui.EndTabBar();
        }
    }

    private void DrawSearchTab(float iconSize, int columns) {
        if (ImGui.BeginTabItem("Search")) {
            if (ImGui.InputTextWithHint("###iconsearch", "Search", ref searchText, 255)) {
                RefreshSearch();
            }
            ImGui.SameLine();
            ImGui.Checkbox("Show Icon Tags", ref showIconNames);
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("WARNING: Icon tags may contain spoilers! Use at your own risk.");
            }

            ImGui.BeginChild("Search##IconList");
            if (namedIconIndex.State == NamedIconIndex.IndexState.INDEXED) {
                DrawSearchResults(iconSize, columns);
            } else {
                var spinner = "|/-\\"[(int)(ImGui.GetTime() / 0.05f) % 3];
                ImGui.Text($"Loading {spinner}");
            }

            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }

    private void DrawSearchResults(float iconSize, int columns) {
        var lineHeight = iconSize + ImGui.GetStyle().ItemSpacing.Y;
        ImGuiClip.ClippedDraw(searchedIconInfo, (namedIcon) => {
            var icon = TextureCache.GetIcon(namedIcon.IconId)!;
            ImGui.Image(icon.ImGuiHandle, new Vector2(iconSize));
            if (ImGui.IsItemHovered()) {
                ImGui.BeginTooltip();
                ImGui.TextUnformatted($"{namedIcon.IconId}");

                if (showIconNames) {
                    var columns = 3;
                    var currentColumn = 0;
                    foreach (var name in namedIcon.Names) {
                        ImGui.TextUnformatted(name);
                        if (currentColumn > columns) {
                            currentColumn = 0;
                        } else {
                            ImGui.SameLine();
                            currentColumn += 1;
                        }
                    }
                }
                ImGui.EndTooltip();
            }
            if (ImGui.IsItemClicked() && CurrentEventScope != null) {
                CurrentIconId = namedIcon.IconId;
                EnqueueEvent(namedIcon.IconId);
                this.IsOpen = false;
            }
        }, columns, lineHeight);
    }

    private void DrawIconTab(IconTab tab, float iconSize, int columns) {
        if (ImGui.BeginTabItem(tab.Name)) {
            ImGui.BeginChild($"{tab.Name}##IconList");

            var allIconIds = tab.IconGroups.SelectMany(g => g.IconIds).ToList();
            var lineHeight = iconSize + ImGui.GetStyle().ItemSpacing.Y;
            ImGuiClip.ClippedDraw(allIconIds, (iconId) => {
                var icon = TextureCache.GetIcon(iconId)!;
                ImGui.Image(icon.ImGuiHandle, new Vector2(iconSize));
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip(iconId.ToString());
                }
                if (ImGui.IsItemClicked() && CurrentEventScope != null) {
                    CurrentIconId = iconId;
                    EnqueueEvent(iconId);
                    this.IsOpen = false;
                }
            }, columns, lineHeight);

            ImGui.EndChild();
            ImGui.EndTabItem();
        }
    }

    private IconTab ActiveIconsTab() {
        var usedIcons = Env.MacroConfig.Root.Values()
            .OfType<MateNode.Macro>()
            .Select(macro => macro.IconId)
            .Distinct()
            .ToList();

        var activeGroup = new IconGroup(usedIcons, "Active");
        return new IconTab(
            "Active",
            new() { activeGroup }
        );
    }
}
