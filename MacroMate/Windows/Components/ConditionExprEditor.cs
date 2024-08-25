using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using ImGuiNET;
using MacroMate.Conditions;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dotnet;
using MacroMate.Extensions.Imgui;

namespace MacroMate.Windows.Components;

public class ConditionExprEditor : IDisposable {
    private ValueConditionEditor conditionEditor = new();

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

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4f, 4f) * ImGuiHelpers.GlobalScale);
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
                ImGui.TableSetupColumn("Conditions", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed);

                ImGui.TableNextRow();

                // Conditions
                ImGui.TableNextColumn();
                foreach (var (opExpr, opExprIndex) in andExpression.opExprs.WithIndex()) {
                    ImGui.PushID(opExprIndex);
                    edited |= DrawOpExpr(andIndex, opExprIndex, opExpr, ref conditionExpr);
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
                                    (and) => and.AddCondition(
                                        (conditionFactory.BestInitialValue() ?? conditionFactory.Default()).WrapInDefaultOp()
                                    )
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
                            (and) => and.AddConditions(
                                Env.ConditionManager.CurrentConditions().Enumerate().Select(c => c.WrapInDefaultOp())
                            )
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

    private bool DrawOpExpr(
        int andIndex,
        int opIndex,
        OpExpr op,
        ref ConditionExpr.Or conditionExpr
    ) {
        bool edited = false;

        ImGui.PushID(opIndex);

        var editValueConditionPopup = DrawEditValueConditionPopup(andIndex, opIndex, ref conditionExpr, ref edited);

        var conditionActive = op.SatisfiedBy(Env.ConditionManager.CurrentConditions());
        if (conditionActive) {
            ImGui.PushStyleColor(ImGuiCol.Text, Colors.ActiveGreen);
        } else {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);
        }

        ImGui.AlignTextToFramePadding();
        if (opIndex > 0) {
            ImGui.Text("and");
            ImGui.SameLine();
        }

        ImGui.Text(op.Condition.ConditionName);
        ImGui.SameLine();
        if (ImGui.IsItemHovered()) {
            var currentValue = op.Condition.FactoryRef.Current()?.ValueName;
            if (currentValue != null && currentValue != "") {
                ImGui.SetTooltip(currentValue);
            }
        }

        edited |= DrawOperatorSegment(andIndex, opIndex, op, ref conditionExpr);
        ImGui.SameLine();

        if (op.Condition is IValueCondition valueCondition) {
            DrawValueConditionButton(editValueConditionPopup, andIndex, opIndex, op, valueCondition, ref conditionExpr);
        } else if (op.Condition is INumericCondition numCondition) {
            edited |= DrawNumConditionInputText(andIndex, opIndex, op, numCondition, ref conditionExpr);
        }

        if (conditionActive) {
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

        ImGui.PopID();

        return edited;
    }

    private bool DrawOperatorSegment(
        int andIndex,
        int opIndex,
        OpExpr op,
        ref ConditionExpr.Or conditionExpr
    ) {
        bool edited = false;

        var opOptions = OpExpr.WrapAll(op.Condition).ToList();
        if (opOptions.Count <= 1) {
            ImGui.Text(op.Text);
            ImGuiExt.HoverTooltip(op.LongText);
            return edited;
        }

        // Remove the "Active Green" colouring if it's active
        ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.Text]);

        var flags = ImGuiComboFlags.NoArrowButton;
        var current = op.Text;
        ImGui.SetNextItemWidth(ImGui.CalcTextSize(op.Text).X + ImGui.GetStyle().FramePadding.X * 2);
        if (ImGui.BeginCombo($"###condition_expr_editor/draw_operator_segment/{andIndex}/{opIndex}", current, flags)) {
            foreach (var opOption in opOptions) {
                if (ImGui.Selectable(opOption.Text, opOption == op)) {
                    conditionExpr = conditionExpr.UpdateAnd(
                        andIndex,
                        (and) => and.SetOperator(opIndex, opOption)
                    );
                    edited = true;
                }
            }
            ImGui.EndCombo();
        }
        ImGuiExt.HoverTooltip(op.LongText);

        ImGui.PopStyleColor();

        return edited;
    }

    private void DrawValueConditionButton(
        uint editValueConditionPopup,
        int andIndex,
        int opIndex,
        OpExpr op,
        IValueCondition condition,
        ref ConditionExpr.Or conditionExpr
    ) {
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0f, 4f) * ImGuiHelpers.GlobalScale);
        if (ImGui.Button(condition.ValueName)) {
            ImGui.OpenPopup(editValueConditionPopup);
            conditionEditor.ConditionFactory = condition.FactoryRef;
            conditionEditor.TrySelect(condition);
        };
        ImGui.PopStyleVar();
    }

    private bool DrawNumConditionInputText(
        int andIndex,
        int opIndex,
        OpExpr op,
        INumericCondition condition,
        ref ConditionExpr.Or conditionExpr
    ) {
        bool edited = false;
        var rightClickPopup = DrawNumConditionPopup(andIndex, opIndex, op, condition, ref conditionExpr, ref edited);

        var num = condition.AsNumber();
        ImGui.SetNextItemWidth(ImGui.CalcTextSize(num.ToString() + "  ").X + ImGui.GetStyle().FramePadding.X * 2);
        if (ImGuiExt.InputTextInt($"###condition_expr_editor/num_condition_input_text/{andIndex}/{opIndex}", ref num)) {
            conditionExpr = conditionExpr.UpdateAnd(
                andIndex,
                (and) => and.SetCondition(opIndex, condition.FactoryRef.FromNumber(num))
            );
            edited = true;
        }
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
            ImGui.OpenPopup(rightClickPopup);
        }
        return edited;
    }

    private uint DrawNumConditionPopup(
        int andIndex,
        int opIndex,
        OpExpr op,
        INumericCondition condition,
        ref ConditionExpr.Or conditionExpr,
        ref bool edited
    ) {
        var popupName = $"###condition_expr_editors/num_condition_popup/{andIndex}/{opIndex}";
        var popupId = ImGui.GetID(popupName);

        if (ImGui.BeginPopup(popupName)) {
            if (ImGui.Selectable("Select Current")) {
                var current = condition.FactoryRef.Current();
                if (current != null) {
                    conditionExpr = conditionExpr.UpdateAnd(
                        andIndex,
                        (and) => and.SetCondition(opIndex, current)
                    );
                    edited = true;
                }
            }
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Select the currently active condition");
            }

            if (ImGui.Selectable("Delete")) {
                conditionExpr = conditionExpr.UpdateAnd(
                    andIndex,
                    (and) => and.DeleteCondition(opIndex)
                );
                edited = true;
            }
            ImGui.EndPopup();
        }

        return popupId;
    }

    private uint DrawEditValueConditionPopup(
        int andIndex,
        int opIndex,
        ref ConditionExpr.Or conditionExpr,
        ref bool edited
    ) {
        var popupName = $"###condition_expr_editor/edit_condition_popup/{andIndex}/{opIndex}";
        var popupId = ImGui.GetID(popupName);

        if (ImGui.BeginPopup(popupName)) {
            var drawResult = conditionEditor.Draw();
            if (drawResult == ValueConditionEditor.DrawResult.Edited && conditionEditor.SelectedCondition != null) {
                conditionExpr = conditionExpr.UpdateAnd(
                    andIndex,
                    (and) => and.SetCondition(opIndex, conditionEditor.SelectedCondition)
                );
                edited = true;
            } else if (drawResult == ValueConditionEditor.DrawResult.DeleteRequested) {
                conditionExpr = conditionExpr.UpdateAnd(
                    andIndex,
                    (and) => and.DeleteCondition(opIndex)
                );
                edited = true;
            }
            ImGui.EndPopup();
        }

        return popupId;
    }
}
