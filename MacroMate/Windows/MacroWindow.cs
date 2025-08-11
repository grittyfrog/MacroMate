using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using MacroMate.Extensions.Imgui;
using MacroMate.Windows.Components;
using MacroMate.MacroTree;
using MacroMate.Extensions.Dalamud.Macros;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dalamud.Str;
using MacroMate.Extensions.Dalamaud.Interface.Components;
using Dalamud.Interface.Utility;
using System.Linq;
using Dalamud.Game.Text;

namespace MacroMate.Windows;

public class MacroWindow : Window, IDisposable {
    public static readonly string NAME = "MacroMateMacroWindow";

    public MateNode.Macro? Macro { get; private set; }

    private ConditionExprEditor conditionExprEditor = new();
    private SeStringInputTextMultiline seStringInputTextMultiline = new();

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
        WindowName = $"{Macro.PathTo()}###{NAME}";
        Env.PluginWindowManager.ShowOrFocus(this);
    }

    public bool IsEditing(MateNode.Macro macro) {
        return this.IsOpen && Macro != null && Macro.Id == macro.Id;
    }

    public override void Draw() {
        if (Macro == null) { return; }

        DrawMenuBar();

        if (
            ImGui.BeginTable(
                "layoutTable",
                3,
                ImGuiTableFlags.Hideable | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX | ImGuiTableFlags.Resizable
            )
        ) {
            ImGui.TableSetupColumn("Macro", ImGuiTableColumnFlags.WidthFixed, 500f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("Link Macros", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, MacroLinkEditor.Width);
            ImGui.TableSetupColumn("Link Conditions", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.NoResize);

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

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8.0f * ImGuiHelpers.GlobalScale, ImGui.GetStyle().ItemSpacing.Y));
        if (ImGui.BeginMenuBar()) {
            MacroRunMenuItem.Draw(
                Macro,
                showLinesOnSingleRunHover: false,
                showLinesOnMultiRunHover: true
            );

            // Link Item
            if (ImGui.MenuItem($"Link ({Macro!.Link.Name()})", selected: showLinkMacros)) {
                showLinkMacros = !showLinkMacros;
            }
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                ImGui.SetTooltip("Link this macro to one or more in-game macro slots");
            }

            // Conditions Item
            if (ImGui.MenuItem("Link Conditions", selected: showLinkConditions)) {
                showLinkConditions = !showLinkConditions;
            }
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                ImGui.SetTooltip("Define when to link this macro to the in-game macro slots");
            }

            if (ImGui.BeginMenu("Settings")) {
                if (Env.PluginInterface.MacroChainPluginIsLoaded()) {
                    var linkWithMacroChain = Macro.LinkWithMacroChain;
                    if (ImGui.Checkbox("Use Macro Chain", ref linkWithMacroChain)) {
                        Macro.LinkWithMacroChain = linkWithMacroChain;
                        edited = true;
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip("Use Macro Chain when binding long macros. Macros must be linked to adjacent positions for this to work");
                    }
                } else {
                    ImGui.BeginDisabled();
                    var disabled = false;
                    if (ImGui.Checkbox("Use Macro Chain", ref disabled)) {
                        disabled = false;
                    };
                    ImGui.EndDisabled();
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                        ImGui.SetTooltip("Use Macro Chain when binding long macros. This setting is only availalbe if Macro Chain is installed.");
                    }
                }
                ImGui.EndMenu();
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

        var rightClickPopup = DrawMacroTextRightClickPopup(Macro);

        bool edited = false;

        // Macro Icon and Name
        if (IconAndName.Draw(Macro)) {
            edited = true;
        }

        var lines = Macro!.Lines;
        if (seStringInputTextMultiline.Draw(
            "###macro-lines",
            ref lines,
            ushort.MaxValue, // Allow for many lines, since we chunk them by blocks of 15 for execution/binding.
            new Vector2(
                MathF.Min(ImGui.GetContentRegionAvail().X, 500f * ImGuiHelpers.GlobalScale),
                ImGui.GetTextLineHeight() * 20
            ),
            // Don't allow lines that are longer then the max line length
            ImGuiInputTextFlags.CallbackCharFilter,
            ImGuiExt.CallbackCharFilterFn(_ => lines.MaxLineLength() < VanillaMacro.MaxLineLength)
        )) {
            Macro.Lines = lines;
        }
        edited = edited || ImGui.IsItemDeactivatedAfterEdit();
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
            ImGui.OpenPopup(rightClickPopup);
        }

        if (edited) {
            WindowName = $"{Macro.PathTo()}###{NAME}";
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

        var edited = false;
        var alwaysLinked = Macro!.AlwaysLinked;
        if (ImGui.Checkbox("Always Linked", ref alwaysLinked)) {
            edited |= true;
        }
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Ignore all conditions and always link this macro");
        }

        var conditionExpr = Macro!.ConditionExpr;
        if (!alwaysLinked) {
            edited |= conditionExprEditor.DrawEditor(ref conditionExpr);
        }

        if (edited) {
            Macro.AlwaysLinked = alwaysLinked;
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

    private uint DrawMacroTextRightClickPopup(MateNode.Macro macro) {
        var macroTextRightClickPopupName = $"""macrowindow/macro_text_right_click_popup/{macro.Id}""";
        var macroTextRightClickPopupId = ImGui.GetID(macroTextRightClickPopupName);

        if (ImGui.BeginPopup(macroTextRightClickPopupName)) {
            if (ImGui.Selectable("Run Selected")) {
                var selected = seStringInputTextMultiline.SelectedSeString();
                if (selected != null) {
                    Env.VanillaMacroManager.ExecuteMacro(selected);
                }
            }
            if (ImGui.IsItemHovered()) {
                var selected = seStringInputTextMultiline.SelectedSeString();
                if (selected != null) {
                    ImGui.SetTooltip(selected.TextValue.ReplaceLineEndings());
                }
            }

            if (ImGui.Selectable("Insert Special Character")) {
                OpenMacroSpecialCharacterPopup(macro);
            }

            ImGui.EndPopup();
        }

        return macroTextRightClickPopupId;
    }

    private void OpenMacroSpecialCharacterPopup(MateNode.Macro macro) {
        var seSpecialChars = Enum.GetValues<SeIconChar>()
            .Select(c => c.ToIconChar())
            .ToList();

        var seUnicodeChars = """
        π™′＾¿¿‰øØ×∞∩£¥¢Ð€ªº†‡¤ ŒœÅ
        ωψ↑↓→←⇔⇒♂♀♪¶§±＜＞≥≤≡÷½¼¾©®ª¹²³
        ※⇔｢｣«»≪≫《》【】℉℃‡。·••‥…¨°º‰
        ╲╳╱☁☀☃♭♯✓〃¹²³
        ●◎○■□▲△▼▽∇♥♡★☆◆◇♦♦♣♠♤♧¶
        αß∇ΘΦΩδ∂∃∀∈∋∑√∝∞∠∟∥∪∩∨∧∫∮∬
        ∴∵∽≠≦≤≥≧⊂⊃⊆⊇⊥⊿⌒─━│┃│¦
        ┗┓└┏┐┌┘┛├┝┠┣┤┥┫┬┯┰┳┴┷┸┻╋
        ┿╂┼￢￣，－．／：；＜＝＞［＼］＿｀｛｜｝～＠
        ⑴⑵⑶⑷⑸⑹⑺⑻⑼⑽⑾⑿⒀⒁⒂⒃⒄⒅⒆⒇
        ⓪①②③④⑤⑥⑦⑧⑨⑩⑪⑫⑬⑭⑮⑯⑰⑱⑲⑳
        №ⅠⅡⅢⅣⅤⅥⅦⅧⅨⅩⅰⅱⅲⅳⅴⅵⅶⅷⅸⅹ
        ０１２３４５６７８９！？＂＃＄％＆＇（）＊＋￠￤￥
        ＡＢＣＤＥＦＧＨＩＪＫＬＭＮＯＰＱＲＳＴＵＶＷＸＹＺ
        ａｂｃｄｅｆｇｈｉｊｋｌｍｎｏｐｑｒｓｔｕｖｗｘｙｚ
        """.Where(c => !Char.IsWhiteSpace(c));

        var specialChars = seSpecialChars.Concat(seUnicodeChars).ToList();

        Env.PluginWindowManager.CharPicker.Open(specialChars, (choice) => {
            macro.Lines = macro.Lines.InsertAtTextValueIndex(
                choice.ToString(),
                (seStringInputTextMultiline.GetCursorPos() ?? 0) + Env.PluginWindowManager.CharPicker.ConsecutiveInserts
            );
            Env.MacroConfig.NotifyEdit();
        });
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
