using System;
using System.Linq;
using System.Text;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace MacroMate.Windows;

public class HelpWindow : Window {
    public static readonly string NAME = "Help";

    public HelpWindow() : base(NAME) {}

    public void ShowOrFocus() {
        Env.PluginWindowManager.ShowOrFocus(this);
    }

    public override void Draw() {
        DrawGeneralHelp();
    }

    private void DrawGeneralHelp() {
        DrawPseudoMarkdown(@"
Introduction
===
Macro Mate lets you store and run an unlimited number of macros. You can also bind macros to normal macro slots.

For example, you can:

- Automatically swap in the correct raid macro when you enter an instance
- Store longer macros and automatically split them across multiple macro slots.
- Use almost any icon in the game for your Macros


Binding
===
Macros will automatically bind to their linked macro slots when its link conditions are satisifed.

For example, a Macro that is linked to Individual Slot 1 with a Link Condition of 'Job is DRG' would
be copied to Individual Macro Slot 1 when you change your class to Dragoon.

Macros can be linked to multiple slots. This is used to allow for longer macros, as each slot can only
accomodate 15 lines. Longer macros will be written to each linked slot in sequence.

If multiple Macros want to bind to the same slot then the one that is higher up in the tree will
be bound.


Limitations
===
Macro Mate does not extend the macro system in any way and macros are executed identically to the base game.

You can store macros that are longer then 15 lines, but macros must still be
executed in 15-line chunks, and longer macros will need to be linked to multiple macro slots.

Macro Mate is not designed for conditional combat actions. Conditions that are primarily
used for dynamically swapping combat actions are out of scope and will not be introduced in this
plugin.
        ");
    }

    /**
     * Markdown-ish string to ImGui code:
     *
     * Separators with ===
     * Dot lists with `-`
     * Two or more consecutive newlines = Visual newline.
     *
     * Anything fancier? Just draw it normally.
     */
    private void DrawPseudoMarkdown(string text) {
        var lines = text
            .Trim()
            .Split("\n");

        // Find all the double-newlines and replace them with "\n" so we don't
        // need to do any lookahead in our normal loop
        var linesCollapsed = lines.Zip(lines.Skip(1), (line, nextLine) => line == "" && nextLine == "" ? "\n" : line);

        var currentText = new StringBuilder(); // Accumulate text until we hit a newline or special char
        foreach (var line in linesCollapsed) {
            if (line == "===") {
                TextWrappedAndClear(currentText);
                ImGui.Separator();
                continue;
            } else if (line.StartsWith("- ")) {
                TextWrappedAndClear(currentText);
                ImGui.BulletText(line.Substring(2));
                continue;
            } else if (line == "\n") { // parsed double-empty-line
                TextWrappedAndClear(currentText);
                ImGui.NewLine();
                continue;
            } else if (line == "") {
                TextWrappedAndClear(currentText);
                continue;
            }

            currentText.Append(line);
        }

        TextWrappedAndClear(currentText);
    }

    private void TextWrappedAndClear(StringBuilder builder) {
        if (builder.ToString() != "") {
            ImGui.TextWrapped(builder.ToString());
        }
        builder.Clear();
    }
}
