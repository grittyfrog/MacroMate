using System;
using System.Collections.Generic;
using System.Linq;
using MacroMate.Conditions;
using MacroMate.Extensions.Dotnet;
using ImGuiNET;

namespace MacroMate.Windows.Components;

public class ValueConditionEditor : IDisposable {
    // Some day we might support more then two levels of narrowing, today is not that day.
    private ValueConditionEditorColumn topLevelColumn = new();
    private ValueConditionEditorColumn narrowColumn = new();

    public IValueCondition? SelectedCondition => narrowColumn.SelectedCondition ?? topLevelColumn.SelectedCondition;

    /// Sets the SelectedCondition of our columns to condition if possible.
    ///
    /// Make sure to provide ConditionFactory before calling this function.
    public void TrySelect(IValueCondition condition) {
        bool topLevelUpdated = topLevelColumn.TrySelect(condition);

        if (topLevelUpdated && topLevelColumn.SelectedCondition != null) {
            narrowColumn.Conditions = topLevelColumn.SelectedCondition.FactoryRef
                .Narrow(topLevelColumn.SelectedCondition)
                .ToList();
            narrowColumn.TrySelect(condition);
        }
    }

    private IValueCondition.IFactory? _conditionFactory = null;
    public IValueCondition.IFactory? ConditionFactory {
        get { return _conditionFactory; }
        set {
            if (_conditionFactory != value) {
                _conditionFactory = value;
                topLevelColumn.Conditions = _conditionFactory?.TopLevel() ?? new List<IValueCondition>();
                narrowColumn.Conditions = new List<IValueCondition>();
            }
        }
    }

    public enum DrawResult { None, Edited, DeleteRequested }
    public DrawResult Draw() {
        bool edited = false;

        if (ImGui.BeginTable("condition_editor_layout_table", 2)) {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.PushID("top_level");
            var topLevelUpdated = topLevelColumn.Draw();
            if (topLevelUpdated && topLevelColumn.SelectedCondition != null) {
                narrowColumn.Conditions = topLevelColumn.SelectedCondition.FactoryRef
                    .Narrow(topLevelColumn.SelectedCondition)
                    .ToList();
                narrowColumn.TrySelect(null);
            }
            ImGui.PopID();

            ImGui.TableNextColumn();
            var narrowUpdated = false;
            if (narrowColumn.Conditions.Count() > 0) {
                ImGui.PushID("narrow");
                narrowUpdated = narrowColumn.Draw();
                ImGui.PopID();
            }

            ImGui.EndTable();
            edited = topLevelUpdated || narrowUpdated;
        }

        if (ImGui.Button("Select Current")) {
            var current = ConditionFactory?.Current();
            if (current != null) {
                TrySelect(current);
                edited = true;
            }
        }
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Select the currently active condition");
        }

        ImGui.SameLine();

        if (ImGui.Button("Delete")) {
            return DrawResult.DeleteRequested;
        }

        return edited ? DrawResult.Edited : DrawResult.None;
    }

    public void Dispose() {
        topLevelColumn.Dispose();
        narrowColumn.Dispose();
    }
}

unsafe class ValueConditionEditorColumn : IDisposable {
    private ImGuiTextFilterPtr filter;
    private ImGuiListClipperPtr clipper;
    private int? scrollToIndexOnNextDraw = null;

    private IEnumerable<IValueCondition> _conditions = new List<IValueCondition>();
    public IEnumerable<IValueCondition> Conditions {
        get { return _conditions; }
        set {
            if(_conditions != value) {
                _conditions = value;
                RefilterConditions();
            }
        }
    }

    public IValueCondition? SelectedCondition { get; private set; } = null;

    public bool TrySelect(IValueCondition? selectCondition) {
        if (selectCondition == null) {
            SelectedCondition = null;
            return false;
        }

        var (matchingCondition, matchingConditionIndex) = filteredConditions
            .WithIndex()
            .FirstOrDefault((conditionAndIndex) => conditionAndIndex.item.SatisfiedBy(selectCondition));

        if (matchingCondition != null) {
            SelectedCondition = matchingCondition;
            scrollToIndexOnNextDraw = matchingConditionIndex;
        }
        return true;
    }

    private IEnumerable<IValueCondition> filteredConditions = new List<IValueCondition>();

    public ValueConditionEditorColumn() {
        var filterPtr = ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null);
        filter = new ImGuiTextFilterPtr(filterPtr);
        clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
    }

    /// Draws the Condition Window and returns true if something was selected
    public bool Draw() {
        bool updated = false;

        ImGui.PushItemWidth(350f);
        if (filter.Draw("")) { RefilterConditions(); };

        if (ImGui.BeginListBox("###condition_editor")) {
            clipper.Begin(filteredConditions.Count());
            while (clipper.Step()) {
                var conditionRange = filteredConditions
                    .Skip(clipper.DisplayStart)
                    .Take(clipper.DisplayEnd - clipper.DisplayStart);
                foreach (var (condition, cindex) in conditionRange.WithIndex()) {
                    ImGui.PushID(cindex);
                    if (
                        ImGui.Selectable(
                            condition.NarrowName,
                            condition.Equals(SelectedCondition),
                            ImGuiSelectableFlags.DontClosePopups
                        )
                    ) {
                        SelectedCondition = condition;
                        updated = true;
                    }
                    ImGui.PopID();
                }
            }

            if (scrollToIndexOnNextDraw != null) {
                var scrollHeight = clipper.ItemsHeight * scrollToIndexOnNextDraw.GetValueOrDefault();
                ImGui.SetScrollY(scrollHeight);
                scrollToIndexOnNextDraw = null;
            }

            clipper.End();
            ImGui.EndListBox();
        };
        return updated;
    }

    private void RefilterConditions() {
        // We have to pre-filter our data for clipper to work nicely.
        filteredConditions = Conditions
            .AsParallel()
            .AsOrdered()
            .Where(condition => filter.PassFilter(condition.NarrowName)).ToList();
    }

    public void Dispose() {
        ImGuiNative.ImGuiTextFilter_destroy(filter.NativePtr);
    }
}
