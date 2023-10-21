using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using MacroMate.Extensions.Imgui;
using MacroMate.Windows.Components;
using MacroMate.MacroTree;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Windows;

public class MacroWindow : Window, IDisposable {
    public static readonly string NAME = "Macro";

    public MateNode.Macro? Macro { get; set; }

    private ConditionExprEditor conditionExprEditor = new();

    private bool showLinkMacros = false;
    private bool showLinkConditions = false;

    private bool deleteRequested = false;

    public MacroWindow() : base(
        NAME,
        ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.AlwaysAutoResize
    ) {}

    public void Dispose() {
        conditionExprEditor.Dispose();
    }

    public void ShowOrFocus(MateNode.Macro macro) {
        Macro = macro;
        Env.PluginWindowManager.RightAlignedShowOrFocus(this);
    }

    public override void Draw() {
        if (Macro == null) { return; }

        DrawMenuBar();

        if (
            ImGui.BeginTable(
                "layoutTable",
                3,
                ImGuiTableFlags.Hideable | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX
            )
        ) {
            ImGui.TableSetupColumn("Macro", ImGuiTableColumnFlags.WidthFixed, 500f);
            ImGui.TableSetupColumn("Link Macros", ImGuiTableColumnFlags.WidthFixed, 400f);
            ImGui.TableSetupColumn("Link Conditions", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableSetColumnEnabled(1, showLinkMacros);
            ImGui.TableSetColumnEnabled(2, showLinkConditions);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            DrawMacro();

            ImGui.TableNextColumn();
            DrawMacroLinkEditor();

            ImGui.TableNextColumn();
            DrawConditionExprEditor();

            ImGui.EndTable();
        }
    }

    private void DrawMenuBar() {
        if (Macro == null) { return; }

        bool edited = false;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8.0f, ImGui.GetStyle().ItemSpacing.Y));
        if (ImGui.BeginMenuBar()) {
            MacroRunMenuItem.Draw(
                Macro,
                showLinesOnSingleRunHover: false,
                showLinesOnMultiRunHover: true
            );

            // Link Item
            if (ImGui.MenuItem($"Link ({Macro!.Link.Name()})", null, showLinkMacros)) {
                showLinkMacros = !showLinkMacros;
            }
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                ImGui.SetTooltip("Link this macro to one or more in-game macro slots");
            }

            // Conditions Item
            if (ImGui.MenuItem("Link Conditions", null, showLinkConditions)) {
                showLinkConditions = !showLinkConditions;
            }
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                ImGui.SetTooltip("Define when to link this macro to the in-game macro slots");
            }

            // Delete Item
            if (ImGui.MenuItem("Delete")) {
                ImGui.OpenPopup("macrowindow_delete_popup");
            }
            if (ImGui.BeginPopup("macrowindow_delete_popup")) {
                if (ImGui.Selectable("Delete!")) {
                    deleteRequested = true;
                    edited = true;
                }
                ImGui.EndPopup();
            }
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) { ImGui.SetTooltip("Delete"); }

            ImGui.EndMenuBar();
        }
        ImGui.PopStyleVar();

        if (edited == true) {
            Save();
        }
    }

    private void DrawMacro() {
        if (Macro == null) { return; }

        bool edited = false;

        // Macro Icon and Name
        if (IconAndName.Draw(Macro)) {
            edited = true;
        }

        var lines = Macro!.Lines;
        if (ImGui.InputTextMultiline(
            $"###macro-lines",
            ref lines,
            ushort.MaxValue, // Allow for many lines, since we chunk them by blocks of 15 for execution/bindingn.
            new Vector2(
                MathF.Min(ImGui.GetContentRegionAvail().X, 500f),
                ImGui.GetTextLineHeight() * 20
            ),
            // Don't allow lines that are longer then the max line length
            ImGuiInputTextFlags.CallbackCharFilter,
            ImGuiExt.CallbackCharFilterFn(_ => lines.MaxLineLength() < VanillaMacro.MaxLineLength)
        )) {
            Macro.Lines = lines;
        };
        edited = edited || ImGui.IsItemDeactivatedAfterEdit();

        if (edited) {
            Save();
        }
    }

    private void DrawConditionExprEditor() {
        ImGui.PushID("macro_draw_condition_expr_editor");
        ImGui.Text("Link Conditions");
        ImGui.SameLine();
        ImGuiExt.HelpMarker(
            String.Join(
                "\n",
                "This macro will be linked to the configured macro slot if ANY of the below conditions are true.",
                "If multiple macros are linked to the same slot, the first one in the tree is chosen."
            )
        );

        var conditionExpr = Macro!.ConditionExpr;
        if (conditionExprEditor.DrawEditor(ref conditionExpr)) {
            Macro.ConditionExpr = conditionExpr;
            Save();
        };
        ImGui.PopID();
    }

    private void DrawMacroLinkEditor() {
        ImGui.PushID("macro_draw_link_editor");
        var macroLink = Macro!.Link;
        if (MacroLinkEditor.DrawEditor(ref macroLink)) {
            Macro.Link = macroLink.Clone();
            Save();
        }
        ImGui.PopID();
    }

    private uint DrawRunMacroPopup(MateNode.Macro macro) {
        var runMacroPopupName = $"###macrowindow/run_macro_popup/{macro.Id}";
        var runMacroPopupId = ImGui.GetID(runMacroPopupName);

        if (ImGui.BeginPopup(runMacroPopupName)) {
            foreach (var vanillaMacro in Macro!.VanillaMacros()) {
                if (ImGui.Selectable(vanillaMacro.Title)) {
                    Env.VanillaMacroManager.ExecuteMacro(vanillaMacro);
                }
            }
            ImGui.EndPopup();
        }

        return runMacroPopupId;
    }


    private void Save() {
        if (deleteRequested) {
            deleteRequested = false;
            Env.MacroConfig.Delete(Macro!);
            Env.PluginWindowManager.MacroWindow.IsOpen = false;
        }

        Env.MacroConfig.NotifyEdit();
    }
}
