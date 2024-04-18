using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace MacroMate.Extensions.Dalamud.Interface.ImGuiIconPicker;

/// Class to pick the icon for a macro.
///
/// Inspirations:
/// - QoLBar's IconBrowser: https://github.com/UnknownX7/QoLBar/blob/f80d64ab064e4e7574a42b2efc678beebdcb1af9/UI/IconBrowserUI.cs
/// - Dalamud's IconBrowserWidget: https://github.com/goatcorp/Dalamud/blob/deef16cdd742ca9faa403e388602795e9d3b54e9/Dalamud/Interface/Internal/Windows/Data/Widgets/IconBrowserWidget.cs#L16
public class IconPickerDialog : Window, IDisposable {
    public static readonly string NAME = "Icon Picker";

    private Action<uint>? Callback { get; set; }
    private uint? CurrentIconId { get; set; }

    private IconPickerConcurrentIconLoader TextureLoader = new(Env.TextureProvider);
    private IconInfoIndex iconInfoIndex = new();

    private string searchText = "";
    private List<IconInfo> searchedIconInfo = new();

    private IconInfoCategory? _selectedCategory = null;
    private IconInfoCategory? selectedCategory {
        get { return _selectedCategory; }
        set {
            _selectedCategory = value;
            RefreshSearch();
        }
    }

    private bool showIconNames = false;

    public IconPickerDialog() : base(NAME, ImGuiWindowFlags.NoScrollbar) {
        this.SizeConstraints = new WindowSizeConstraints {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        iconInfoIndex.StartIndexing(() => {
            RefreshSearch();
        });
    }

    private void RefreshSearch() {
        if (searchText == "") {
            searchedIconInfo = iconInfoIndex.All(selectedCategory);
        } else {
            searchedIconInfo = iconInfoIndex.NameSearch(searchText, selectedCategory);
        }
    }

    public override void OnClose() {
        base.OnClose();
        TextureLoader.Clear();
    }

    public void Open(Action<uint> callback) => Open(null, callback);
    public void Open(uint? initialIcon, Action<uint> callback) {
        CurrentIconId = initialIcon;
        Callback = callback;
        if (!IsOpen) {
            IsOpen = true;
        } else {
            ImGui.SetWindowFocus(WindowName);
        }
    }

    public void Dispose() {
        TextureLoader.Dispose();
    }

    public override void Draw() {
        float iconSize = 48 * ImGuiHelpers.GlobalScale;

        if (ImGui.BeginTable("icon_picker_layout_table", 2, ImGuiTableFlags.Resizable)) {
            ImGui.TableSetupColumn("CategoryTree", ImGuiTableColumnFlags.WidthFixed, 150.0f);
            ImGui.TableSetupColumn("Icons", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TableNextColumn();

            var searchHint = selectedCategory == null ? "Search" : $"Search {selectedCategory}";
            if (ImGui.InputTextWithHint("###iconsearch", searchHint, ref searchText, 255)) {
                RefreshSearch();
            }

            ImGui.SameLine();
            ImGui.Checkbox("Show Icon Tags", ref showIconNames);
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("WARNING: Icon tags may contain spoilers! Use at your own risk.");
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            ImGui.BeginChild("Search##CategoryTree");
            var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick
                | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
            if (selectedCategory == null) { flags |= ImGuiTreeNodeFlags.Selected; }
            ImGui.TreeNodeEx("All", flags);
            if (ImGui.IsItemClicked()) {
                selectedCategory = null;
                ImGui.CloseCurrentPopup();
            }
            foreach (var categoryTree in iconInfoIndex.CategoryRoots()) {
                DrawCategoryGroupTree(categoryTree);
            }
            ImGui.EndChild();

            ImGui.TableNextColumn();
            ImGui.BeginChild("Search##IconList");
            var columns = (int)((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X) / (iconSize + ImGui.GetStyle().ItemSpacing.X));
            if (iconInfoIndex.State == IconInfoIndex.IndexState.INDEXED) {
                DrawSearchResults(iconSize, columns);
            } else {
                var spinner = "|/-\\"[(int)(ImGui.GetTime() / 0.05f) % 3];
                ImGui.Text($"Indexing... {spinner}");
            }

            ImGui.EndChild();

            ImGui.EndTable();
        }
    }

    private void DrawCategoryGroupTree(IconInfoCategoryGroup categoryGroup) {
        ImGui.PushID(categoryGroup.Category.ToString());
        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.SpanAvailWidth;
        if (selectedCategory == categoryGroup.Category) {
            flags |= ImGuiTreeNodeFlags.Selected;
        }
        if (categoryGroup.SubcategoryIconInfos.Count == 0) {
            flags |= ImGuiTreeNodeFlags.Leaf;
        }

        bool categoryOpen = ImGui.TreeNodeEx(categoryGroup.Category.ToString(), flags);
        if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen()) {
            selectedCategory = categoryGroup.Category;
            ImGui.CloseCurrentPopup();
        }
        if (categoryOpen) {
            foreach (var (subcategory, _) in categoryGroup.SubcategoryIconInfos) {
                var subFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick
                    | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                if (selectedCategory == subcategory) {
                    subFlags |= ImGuiTreeNodeFlags.Selected;
                }

                ImGui.TreeNodeEx(subcategory.ToString(), subFlags);
                if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen()) {
                    selectedCategory = subcategory;
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.TreePop();
        }
        ImGui.PopID();
    }

    private void DrawSearchResults(float iconSize, int columns) {
        var lineHeight = iconSize + ImGui.GetStyle().ItemSpacing.Y;
        ImGuiClip.ClippedDraw(searchedIconInfo, (namedIcon) => {
            var icon = TextureLoader.GetIcon(namedIcon.IconId)!;
            ImGui.Image(
                icon.ImGuiHandle,
                new Vector2(iconSize),
                new Vector2(0.0f, 0.0f),
                new Vector2(1.0f, 1.0f)
            );
            if (ImGui.IsItemHovered()) {
                ImGui.BeginTooltip();

                if (ImGui.IsMouseDown(ImGuiMouseButton.Right)) {
                    // Icon Preview
                    ImGui.Image(icon.ImGuiHandle, new Vector2(700 * ImGuiHelpers.GlobalScale));
                } else {
                    // Icon Details
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
                }
                ImGui.EndTooltip();
            }
            if (ImGui.IsItemClicked() && Callback != null) {
                CurrentIconId = namedIcon.IconId;
                Callback(namedIcon.IconId);
                this.Callback = null;
                this.IsOpen = false;
            }
        }, columns, lineHeight);
    }
}
