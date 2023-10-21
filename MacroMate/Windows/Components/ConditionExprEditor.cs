using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;
using MacroMate.Conditions;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Windows.Components;

public class ConditionExprEditor : IDisposable {
    private ConditionEditor conditionEditor = new();

    public void Dispose() {
        conditionEditor.Dispose();
    }

    public bool DrawEditor(ref ConditionExpr.Or conditionExpr) {
        var edited = false;

        var tableFlags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders;
        if (ImGui.BeginTable("condition_expr_editor_layout_table", 2, tableFlags)) {
            ImGui.TableSetupColumn("Conditions", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed);

            foreach (var (andExpression, andIndex) in conditionExpr.options.WithIndex()) {
                ImGui.PushID(andIndex);
                ImGui.TableNextRow();

                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4f, 4f));

                // Conditions
                ImGui.TableNextColumn();
                foreach (var (condition, conditionIndex) in andExpression.conditions.WithIndex()) {
                    ImGui.PushID(conditionIndex);
                    ImGui.AlignTextToFramePadding();
                    edited |= DrawCondition(andIndex, conditionIndex, condition, ref conditionExpr);
                    ImGui.PopID();
                }

                // Action Button
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Bars.ToIconString())) {
                    ImGui.OpenPopup($"condition_action_button_popup");
                }


                if (ImGui.BeginPopup($"condition_action_button_popup")) {
                    if (ImGui.BeginMenu("Add Condition")) {
                        foreach (var (conditionFactory, cfIndex) in ICondition.IFactory.All.WithIndex()) {
                            ImGui.PushID(cfIndex);
                            if (ImGui.Selectable(conditionFactory.ConditionName)) {
                                conditionExpr = conditionExpr.UpdateAnd(
                                    andIndex,
                                    (and) => and.AddCondition(conditionFactory.Current() ?? conditionFactory.Default())
                                );
                                edited = true;
                            }
                            ImGui.PopID();
                        }
                        ImGui.EndMenu();
                    }

                    if (ImGui.Selectable("Delete##delete_condition_expr_and")) {
                        conditionExpr = conditionExpr.DeleteAnd(andIndex);
                        edited = true;
                    }
                    ImGui.EndPopup();
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }

        // Add a "And" expression button
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus.ToIconString())) {
            conditionExpr = conditionExpr.AddAnd();
            edited = true;
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
            ImGui.SetTooltip("Add Condition Group");
        }

        return edited;
    }

    private bool DrawCondition(
        int andIndex,
        int conditionIndex,
        ICondition condition,
        ref ConditionExpr.Or conditionExpr
    ) {
        bool edited = false;

        ImGui.PushID(conditionIndex);
        var andString = conditionIndex > 0 ? "and " : "";
        ImGui.Text($"{andString}{condition.ConditionName} is");
        ImGui.SameLine();
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 4f));
        if (ImGui.Button(condition.ValueName)) {
            ImGui.OpenPopup("edit_condition_popup");
            conditionEditor.ConditionFactory = condition.FactoryRef;
            conditionEditor.TrySelect(condition);
        };
        ImGui.PopStyleVar();

        if (ImGui.BeginPopup("edit_condition_popup")) {
            var drawResult = conditionEditor.Draw();
            if (drawResult == ConditionEditor.DrawResult.Edited && conditionEditor.SelectedCondition != null) {
                conditionExpr = conditionExpr.UpdateAnd(
                    andIndex,
                    (and) => and.SetCondition(conditionIndex, conditionEditor.SelectedCondition)
                );
                edited = true;
            } else if (drawResult == ConditionEditor.DrawResult.DeleteRequested) {
                conditionExpr = conditionExpr.UpdateAnd(
                    andIndex,
                    (and) => and.DeleteCondition(conditionIndex)
                );
                edited = true;
            }
            ImGui.EndPopup();
        }

        ImGui.PopID();

        return edited;
    }
}
