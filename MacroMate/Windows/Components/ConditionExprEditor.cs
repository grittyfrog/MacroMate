using System;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using ImGuiNET;
using MacroMate.Conditions;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Windows.Components;

public class ConditionExprEditor : IDisposable {
    private ConditionEditor conditionEditor = new();

    public void Dispose() {
        conditionEditor.Dispose();
    }

    public bool DrawEditor(ref bool alwaysLinked, ref ConditionExpr.Or conditionExpr) {
        var edited = false;

        if (ImGui.Checkbox("Always Linked", ref alwaysLinked)) {
            edited = true;
        }
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Ignore all conditions and always link this macro");
        }

        if (!alwaysLinked) {
            edited |= DrawOrCondition(ref conditionExpr);
        }

        return edited;
    }

    private bool DrawOrCondition(ref ConditionExpr.Or conditionExpr) {
        var edited = false;
        var tableFlags = ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4f, 4f));
        foreach (var (andExpression, andIndex) in conditionExpr.options.WithIndex()) {
            ImGui.PushID(andIndex);

            var andExpressionSatisfied = andExpression.SatisfiedBy(Env.ConditionManager.CurrentConditions());
            if (andExpressionSatisfied) {
                ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, Colors.ActiveGreen);
            } else {
                ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, ImGui.GetStyle().Colors[(int)ImGuiCol.TableBorderStrong]);
            }


            if (andIndex != 0) {
                var or = "Or";
                var preOrPos = ImGui.GetCursorPos();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().FramePadding.Y + ImGui.GetStyle().CellPadding.Y);
                ImGui.Text(or);
                ImGui.SetCursorPos(new Vector2(preOrPos.X + ImGui.CalcTextSize(or).X + ImGui.GetStyle().ItemSpacing.X, preOrPos.Y));
            }

            if (ImGui.BeginTable("condition_expr_editor_layout_table", 2, tableFlags)) {
                ImGui.TableSetupColumn("Conditions", ImGuiTableColumnFlags.WidthFixed);
                ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableNextRow();

                // Conditions
                ImGui.TableNextColumn();
                foreach (var (condition, conditionIndex) in andExpression.conditions.WithIndex()) {
                    ImGui.PushID(conditionIndex);
                    edited |= DrawCondition(andIndex, conditionIndex, condition, ref conditionExpr);
                    ImGui.PopID();
                }

                // Action Button
                ImGui.TableNextColumn();
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
                                    (and) => and.AddCondition(conditionFactory.BestInitialValue() ?? conditionFactory.Default())
                                );
                                edited = true;
                            }
                            ImGui.PopID();
                        }
                        ImGui.EndMenu();
                    }

                    if (ImGui.Selectable("Add All Current Conditions")) {
                        conditionExpr = conditionExpr.UpdateAnd(
                            andIndex,
                            (and) => and.AddConditions(Env.ConditionManager.CurrentConditions().Enumerate())
                        );
                        edited = true;
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip("Adds all currently active conditions to this or condition");
                    }

                    if (ImGui.Selectable("Delete##delete_condition_expr_and")) {
                        conditionExpr = conditionExpr.DeleteAnd(andIndex);
                        edited = true;
                    }
                    ImGui.EndPopup();
                }

                ImGui.EndTable();
            }

            ImGui.PopStyleColor();

            ImGui.PopID();
        }
        ImGui.PopStyleVar();

        // Add a "And" expression button
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus.ToIconString())) {
            conditionExpr = conditionExpr.AddAnd();
            edited = true;
        }
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
            ImGui.SetTooltip("Add 'Or' group");
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

        var conditionActive = condition.SatisfiedBy(Env.ConditionManager.CurrentConditions());

        ImGui.PushID(conditionIndex);
        if (conditionActive) {
            ImGui.PushStyleColor(ImGuiCol.Text, Colors.ActiveGreen);
        } else {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
        }

        ImGui.AlignTextToFramePadding();
        if (conditionIndex > 0) {
            ImGui.Text("and");
            ImGui.SameLine();
        }

        ImGui.Text(condition.ConditionName);
        ImGui.SameLine();
        if (ImGui.IsItemHovered()) {
            var currentValue = condition.FactoryRef.Current()?.ValueName;
            if (currentValue != null && currentValue != "") {
                ImGui.SetTooltip(currentValue);
            }
        }

        ImGui.Text("is");
        ImGui.SameLine();

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 4f));
        if (ImGui.Button(condition.ValueName)) {
            ImGui.OpenPopup("edit_condition_popup");
            conditionEditor.ConditionFactory = condition.FactoryRef;
            conditionEditor.TrySelect(condition);
        };
        ImGui.PopStyleVar();

        if (condition.SatisfiedBy(Env.ConditionManager.CurrentConditions())) {
            var yesIcon = Env.TextureProvider.GetFromGameIcon(76574).GetWrapOrEmpty();
            if (yesIcon != null) {
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().FramePadding.Y);
                ImGui.Image(yesIcon.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight()));

                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("This condition is active");
                }
            }
        }

        ImGui.PopStyleColor();

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
