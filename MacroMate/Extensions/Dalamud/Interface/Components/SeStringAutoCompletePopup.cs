using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using MacroMate.Extensions.Dalamud.AutoComplete;
using MacroMate.Extensions.Dotnet;
using MacroMate.Extensions.Imgui;
using Dalamud.Interface.Utility.Raii;
using MacroMate.Extensions.Dalamud;

namespace MacroMate.Extensions.Dalamaud.Interface.Components;

/// <summary>
/// Popup to show 
/// </summary>
/// <remarks>
/// Heavily inspired by Chat2: https://github.com/Infiziert90/ChatTwo/blob/3951c49e1aa9941afbefe855298b663e68bff3e4/ChatTwo/Ui/ChatLogWindow.cs#L1399
/// ImGui implementation also heavily inspired by this comment: https://github.com/ocornut/imgui/issues/718#issuecomment-2539185115
/// </remarks>
public class SeStringAutoCompletePopup {
    private string name = $"se_string_auto_complete_popup";
    private uint? id;

    private string _autoCompleteFilter = "";
    public string AutoCompleteFilter {
        get => _autoCompleteFilter;
        set {
            _autoCompleteFilter = value;
            Completions = Env.CompletionIndex.Search(_autoCompleteFilter).ToList();
        }
    }

    public Vector2? PopupPos { get; set; } = null;

    private List<CompletionInfo> Completions { get; set; } = new();

    private int? _selectedCompletionIndex = null;
    public int? SelectedCompletionIndex {
        get { return _selectedCompletionIndex; }
        set {
            if (value.HasValue) {
                _selectedCompletionIndex = Math.Clamp(value.Value, 0, Math.Max(0, Completions.Count - 1));
            } else {
                _selectedCompletionIndex = null;
            }
            ShouldScrollOnNextDraw = true;
        }
    }

    private Vector2? LastCompletionTooltipSize = null;

    public CompletionInfo? SelectedCompletion =>
        SelectedCompletionIndex.HasValue ? Completions.ElementAtOrDefault(SelectedCompletionIndex.Value) : null;

    private bool ShouldOpenOnNextDraw { get; set; } = false;

    private bool ShouldScrollOnNextDraw { get; set; } = false;

    /// <summary>
    /// Filled when the auto completion is "Selected". Consuming widget should insert these at the
    /// desired completion spot.
    /// </summary>
    public Queue<CompletionInfo> CompletionEvents { get; set; } = new();

    private bool FixCursor { get; set; } = false;

    // We can't use the ImGuiClipper extensions because they don't return a Ptr reference
    private ImGuiListClipperPtr clipper = ImGui.ImGuiListClipper();

    private ImGuiWindowFlags popupFlags =
        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings;

    public void Draw() {
        id ??= ImGui.GetID(name);

        if (ShouldOpenOnNextDraw) {
            ShouldOpenOnNextDraw = false;
            ImGui.OpenPopup(id.Value);
            SelectedCompletionIndex = 0;
        }

        var longestCompletionWidth = Completions
            .Select(c => ImGui.CalcTextSize(c.SeString.ExtractText()).X)
            .DefaultIfEmpty(0)
            .Max() + (ImGui.GetStyle().ItemSpacing.X * 2);
        var longestGroupWidth = Completions
            .Select(c => ImGui.CalcTextSize(c.GroupTitleString).X)
            .DefaultIfEmpty(0)
            .Max() + (ImGui.GetStyle().ItemSpacing.X * 2);

        var windowWidth = Math.Max(
            200 * ImGuiHelpers.GlobalScale,
            longestCompletionWidth + longestGroupWidth + (ImGui.GetStyle().FramePadding.X * 4)
        );

        if (PopupPos.HasValue) { ImGui.SetNextWindowPos(PopupPos.Value); }
        ImGui.SetNextWindowSize(new Vector2(windowWidth, ImGui.GetFontSize() * 10));

        using var popup = ImRaii.Popup(name, popupFlags);
        if (!popup) { return; }

        ImGui.SetNextItemWidth(windowWidth - (ImGui.GetStyle().FramePadding.X * 8));

        if (
            ImGui.InputTextWithHint(
                "##auto-complete-filter",
                "Filter...",
                ref _autoCompleteFilter,
                256,
                ImGuiInputTextFlags.CallbackAlways | ImGuiInputTextFlags.CallbackHistory
                    | ImGuiInputTextFlags.CallbackEdit,
                AutoCompleteFilterCallback
            )
        ) {
            Completions = Env.CompletionIndex.Search(_autoCompleteFilter).ToList();
            SelectedCompletionIndex = 0;
        }

        if (ImGui.IsWindowAppearing()) {
            FixCursor = true;
            ImGui.SetKeyboardFocusHere(-1);
        }

        if (ImGui.IsItemActive()) {
            if (ImGui.IsKeyPressed(ImGuiKey.DownArrow)) {
                SelectedCompletionIndex = SelectedCompletionIndex + 1;
            } else if (ImGui.IsKeyPressed(ImGuiKey.UpArrow)) {
                SelectedCompletionIndex = SelectedCompletionIndex - 1;
            }
        }

        if (ImGui.IsItemDeactivated()) {
            if (ImGui.IsKeyDown(ImGuiKey.Escape)) {
                ImGui.CloseCurrentPopup();
                return;
            }

            var enter = ImGui.IsKeyDown(ImGuiKey.Enter) || ImGui.IsKeyDown(ImGuiKey.KeypadEnter);
            if (Completions.Count > 0 && enter) {
                Complete();
                ImGui.CloseCurrentPopup();
            }
        }

        DrawCompletionResults(longestCompletionWidth, longestGroupWidth);
    }

    private void DrawCompletionResults(float longestCompletionWidth, float longestGroupWidth) {
        using var child = ImRaii.Child("###se_string_auto_complete/completions_layout_child");
        if (!child) { return; }

        using var table = ImRaii.Table("###se_string_auto_complete/completions_layout_table", 2);
        if (!table) { return; }

        var selectedCompletion = SelectedCompletion;

        ImGui.TableSetupColumn("Completion", ImGuiTableColumnFlags.WidthFixed, longestCompletionWidth);
        ImGui.TableSetupColumn("Group", ImGuiTableColumnFlags.WidthFixed, longestGroupWidth);

        // info, min, max
        (CompletionInfo, Vector2, Vector2)? hoveredCompletionData = null;
        (CompletionInfo, Vector2, Vector2)? visibleSelectedCompletionData = null;

        clipper.Begin(Completions.Count());
        while (clipper.Step()) {
            var completionRange = Completions
                .WithIndex()
                .Skip(clipper.DisplayStart)
                .Take(clipper.DisplayEnd - clipper.DisplayStart);
            foreach (var (completion, index) in completionRange) {
                ImGui.PushID(index);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                var selected = selectedCompletion != null
                    && completion.Key == selectedCompletion.Key
                    && completion.Group == selectedCompletion.Group;
                if (ImGui.Selectable(
                        completion.SeString.ExtractText(),
                        selected,
                        ImGuiSelectableFlags.SpanAllColumns
                    )
                ) {
                    SelectedCompletionIndex = index;
                    Complete();
                    ImGui.CloseCurrentPopup();
                }

                // Hovering something takes precedence tooltip-wise, but otherwise
                // we want to show a tooltip for the currently selected item.
                if (ImGui.IsItemHovered()) {
                    hoveredCompletionData = (completion, ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
                }
                if (selected) {
                    visibleSelectedCompletionData = (completion, ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
                }

                ImGui.TableNextColumn();
                var text = completion.GroupTitleString;
                ImGui.SetCursorPosX(
                    ImGui.GetCursorPosX() + ImGui.GetColumnWidth() - ImGui.CalcTextSize(text).X - ImGui.GetStyle().FramePadding.X
                );
                ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
                ImGui.TextUnformatted(text);
                ImGui.PopStyleColor();
                ImGui.PopID();
            }
        }

        var scrollPos = clipper.StartPosY + clipper.ItemsHeight * SelectedCompletionIndex.GetValueOrDefault();
        if (ShouldScrollOnNextDraw) {
            ImGui.SetScrollFromPosY(scrollPos - ImGui.GetWindowPos().Y);
            ShouldScrollOnNextDraw = false;
        }
        clipper.End();

        if (hoveredCompletionData != null || visibleSelectedCompletionData != null) {
            var completionTooltipData = hoveredCompletionData ?? visibleSelectedCompletionData;
            if (completionTooltipData != null) {
                var scrollbarPad = 20 * ImGuiHelpers.GlobalScale;

                var (completionTooltip, min, max) = completionTooltipData.Value;
                if (completionTooltip.HelpText.HasValue) {
                    var selectionSize = max - min;
                    var selectionMidpoint = min + (selectionSize / 2.0f);

                    if (selectionMidpoint.X <= ImGui.GetWindowViewport().Size.X / 2.0f) {
                        var tooltipX = max.X + scrollbarPad;
                        ImGui.SetNextWindowPos(new Vector2(tooltipX, min.Y));
                    } else if (LastCompletionTooltipSize.HasValue) { // Otherwise...
                        var tooltipX = min.X - LastCompletionTooltipSize.Value.X - scrollbarPad;
                        ImGui.SetNextWindowPos(new Vector2(tooltipX, min.Y));
                    }

                    ImRaii.Tooltip().Use(() => {
                        var drawResult = ImGuiHelpers.SeStringWrapped(
                            completionTooltip.HelpText.Value,
                            new SeStringDrawParams() {
                                WrapWidth = 640 * ImGuiHelpers.GlobalScale
                            }
                        );
                        LastCompletionTooltipSize = drawResult.Size;
                    });
                }
            }
        }
    }

    /// <summary>
    /// Open this popup, but only if there are some completions
    /// </summary>
    public void Open() {
        ShouldOpenOnNextDraw = true;
    }

    private void Complete() {
        if (SelectedCompletionIndex.HasValue) {
            CompletionEvents.Enqueue(Completions[SelectedCompletionIndex.Value]);
        }
    }

    private int AutoCompleteFilterCallback(ImGuiInputTextCallbackDataPtr data) {
        if (FixCursor) {
            FixCursor = false;
            data.CursorPos = _autoCompleteFilter.Length;
            data.SelectionStart = data.SelectionEnd = data.CursorPos;
        }

        return 1;
    }
}
