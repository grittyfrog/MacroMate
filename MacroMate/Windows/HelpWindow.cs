using System.Linq;
using System.Text;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using MacroMate.Conditions;
using MacroMate.MacroTree;

namespace MacroMate.Windows;

public class HelpWindow : Window {
    public static readonly string NAME = "Help";
    public HelpWindow() : base(NAME) {}

    public void ShowOrFocus() {
        Env.PluginWindowManager.ShowOrFocus(this);
    }

    public override void Draw() {
        if (ImGui.CollapsingHeader("Introduction", ImGuiTreeNodeFlags.DefaultOpen)) {
            DrawPseudoMarkdown(@"
            Macro Mate lets you store and run an unlimited number of macros. You can also bind macros to normal macro slots.

            For example, you can:
            - Automatically swap in the correct raid macro when you enter an instance
            - Store longer macros and automatically split them across multiple macro slots.
            - Use almost any icon in the game
            ");
        }

        if (ImGui.CollapsingHeader("Binding")) {
            DrawPseudoMarkdown(@"
            Macros will automatically bind to their linked macro slots when its link conditions are satisifed.

            For example, a Macro that is linked to Individual Slot 1 with a Link Condition of 'Job is DRG' would
            be copied to Individual Macro Slot 1 when you change your class to Dragoon.

            Macros can be linked to multiple slots. This is used to allow for longer macros, as each slot can only
            accomodate 15 lines. Longer macros will be written to each linked slot in sequence.

            If multiple Macros want to bind to the same slot then the one that is higher up in the tree will
            be bound.");
        }

        if (ImGui.CollapsingHeader("Auto Translation")) {
            DrawPseudoMarkdown(@"
            Auto translate text can be copied/pasted from the base game into Macro Mate, and from Macro Mate into the base game.

            At this time tab completion is not supported, it may be supported in a future update.
            ");
        }

        if (ImGui.CollapsingHeader("Paths")) {
            DrawPseudoMarkdown(@"
            Paths identify a particular macro or group in the tree. For example '/My Group/My Macro' is a path that identifies a macro called 'My Macro' inside 'My Group'.

            Paths are made of segments separated by slashes. A segment can be:
            - '<name>' matches a node by name (i.e. 'My Macro' matches the node named 'My Macro')
            - '@<index>' matches the N-th child of a group (i.e '@1' matches the second child of root)
            - '<name>@<index>' matches the N-th child with '<name>' (i.e. 'Run@1' matches the second macro named 'Run' under root)

            Examples:");

            if (ImGui.BeginTable("###helpwindow_paths_examples", 2, ImGuiTableFlags.Borders)) {
                ImGui.TableSetupColumn("Command", ImGuiTableColumnFlags.WidthFixed, 150 * ImGuiHelpers.GlobalScale);
                ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted("/");
                ImGui.TableNextColumn(); ImGui.TextUnformatted("Select root");

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted("/My Group");
                ImGui.TableNextColumn(); ImGui.TextUnformatted("Select 'My Group' under root");

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted("My Group");
                ImGui.TableNextColumn(); ImGui.TextUnformatted("Select 'My Group' under root");

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted("My Group/Subgroup");
                ImGui.TableNextColumn(); ImGui.TextUnformatted("Select 'Subgroup' under 'My Group' under root");

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted("@0");
                ImGui.TableNextColumn(); ImGui.TextUnformatted("Select the first child of root");

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted("My Group/@2");
                ImGui.TableNextColumn(); ImGui.TextUnformatted("Select the third child of 'My Group'");

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted("My Group@1");
                ImGui.TableNextColumn(); ImGui.TextUnformatted("Select the second child named 'My Group' under root");

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted("My\\/Group");
                ImGui.TableNextColumn(); ImGui.TextUnformatted("Select the child named 'My/Group' under root");

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted("My\\@Group");
                ImGui.TableNextColumn(); ImGui.TextUnformatted("Select the child named 'My@Group' under root");

                ImGui.TableNextRow();
                ImGui.TableNextColumn(); ImGui.TextUnformatted("My\\\\Group");
                ImGui.TableNextColumn(); ImGui.TextUnformatted("Select the child named 'My\\Group' under root");

                ImGui.EndTable();
            }
            ImGui.NewLine();

            DrawPseudoMarkdown(@"
            Paths may start with a / or not, either way they always start from the root node of the macro tree.
            Paths also treat @ and \ as special characters which must be escaped if used in a macro name.");
        }

        if (ImGui.CollapsingHeader("Limitations")) {
            DrawPseudoMarkdown(@"
            Macro Mate does not extend the macro system in any way and macros are executed identically to the base game.

            You can store macros that are longer then 15 lines, but macros must still be
            executed in 15-line chunks, and longer macros will need to be linked to multiple macro slots.

            Macro Mate is not designed for conditional combat actions. Conditions that are primarily
            used for dynamically swapping combat actions are out of scope and will not be introduced in this
            plugin.");
        }

        if (ImGui.CollapsingHeader("Tutorial")) {
            ImGui.Indent();
            DrawSprintTutorial();
            ImGui.Unindent();
        }
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
            .Split("\n")
            .Select(line => line.Trim());

        // Find all the double-newlines and replace them with "\n" so we don't
        // need to do any lookahead in our normal loop
        //var linesCollapsed = lines.Zip(lines.Skip(1).Append(""), (line, nextLine) => line == "" && nextLine == "" ? "\n" : line);

        var currentText = new StringBuilder(); // Accumulate text until we hit a newline or special char
        foreach (var line in lines) {
            if (line == "===") {
                TextWrappedAndClear(currentText);
                ImGui.Separator();
                continue;
            } else if (line.StartsWith("- ")) {
                TextWrappedAndClear(currentText);
                ImGui.BulletText(line.Substring(2));
                continue;
            } else if (line == "") { // Previously an empty newline
                TextWrappedAndClear(currentText);
                ImGui.NewLine();
                continue;
            }

            currentText.Append(line);
            currentText.Append(" ");
        }

        TextWrappedAndClear(currentText);
    }

    private void TextWrappedAndClear(StringBuilder builder) {
        if (builder.ToString() != "") {
            ImGui.TextWrapped(builder.ToString());
        }
        builder.Clear();
    }

    private void DrawSprintTutorial() {
        if (ImGui.CollapsingHeader("Normal Sprint / PvP / Island Sanctuary macro")) {
            if (ImGui.Button("Import Example")) {
                var defaultSprintMacro = new MateNode.Macro {
                    Name = "Sprint (Default)",
                    IconId = 104,
                    Link = new MacroLink { Set = Extensions.Dalamud.Macros.VanillaMacroSet.INDIVIDUAL, Slots = new() { 99 } },
                    AlwaysLinked = true,
                    Lines = "/ac \"Sprint\" <me>"
                };

                var pvpSprintMacro = new MateNode.Macro {
                    Name = "Sprint (PvP)",
                    IconId = 104,
                    Link = new MacroLink { Set = Extensions.Dalamud.Macros.VanillaMacroSet.INDIVIDUAL, Slots = new() { 99 } },
                    AlwaysLinked = false,
                    ConditionExpr = ConditionExpr.Or.Single(new PvpStateCondition(PvpStateCondition.State.IN_PVP)),
                    Lines = "/pvpac \"Sprint\" <me>"
                };

                var sanctuarySprintMacro = new MateNode.Macro {
                    Name = "Sprint (Island Sanctuary)",
                    IconId = 104,
                    Link = new MacroLink { Set = Extensions.Dalamud.Macros.VanillaMacroSet.INDIVIDUAL, Slots = new() { 99 } },
                    AlwaysLinked = false,
                    ConditionExpr = ConditionExpr.Or.Single(new LocationCondition(1055)),
                    Lines = "/ac \"Duty Action I\""
                };

                var sprintGroup = new MateNode.Group { Name = "Sprint" };
                sprintGroup.Attach(sanctuarySprintMacro);
                sprintGroup.Attach(pvpSprintMacro);
                sprintGroup.Attach(defaultSprintMacro);

                Env.MacroConfig.Root.Attach(sprintGroup);
                Env.MacroConfig.NotifyEdit();
            }
            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Import the final result of this tutorial into your macro config");
            }

            DrawPseudoMarkdown("""
                Lets make a macro that changes to the correct type of sprint depending on your location.

                First lets set up a normal sprint macro in Macro Mate:

                - Create a new group called 'Sprint' using `New > Group`.
                - Add a Macro to the Sprint group by right-clicking it and selecting 'Add Macro'. Open the new macro by clicking it
                - Rename the macro to `Sprint (Default)` and click the 'M' icon and choose a good sprint icon. (Hint: try searching for 'sprint')
                - In the body of the macro type '/ac Sprint <me>'

                You can test this macro by clicking 'Run' in the Macro window. Your character should start sprinting.

                Now lets link the macro to the in-game macro UI:

                - Click 'Link' on the tab bar, the link screen should appear
                - Click on a macro slot to link to. (Recommended: Individual 99)
                - Click on 'Link Conditions' and make sure 'Always Linked' is ticked.

                Now open the in-game macro window. You should see the 'Sprint (Default)' macro linked in the slot you chose.

                Now lets add a conditional PvP Sprint macro:

                - Add a new macro to the Sprint group by right-clicking it and selecting 'Add Macro'.
                - Drag and drop the macro so it is above 'Sprint (Default)'.
                - Open the new macro by clicking it
                - Rename the macro to `Sprint (PvP)` and pick a good icon
                - In the body of the macro type '/pvpac Sprint <me>'
                - Open the 'Link' panel and select the same macro slot we used for Sprint (Default)
                - Open the 'Link Conditions' panel and untick 'Always Linked'
                - Click the '+' button to add an OR group
                - Add a PvP state condition by clicking the Hamburger button > Add Condition > PvP State
                - Click the button after 'PvP State' and select 'In PvP'

                Now we have a PvP macro! Travel to Wolves Den and check the in-game macro UI, the macro should have swapped to the PvP one.

                This works because Macro Mate always selects the first 'active' macro in the tree when filling a linked slot. And since the
                link conditions for 'Sprint (PvP)' are satisfied it will be chosen. If the conditions aren't active then 'Sprint (Default)' will
                be chosen since it is always active.

                Finally we can do something similar for Island Sanctuary:

                - Add a new macro to the Sprint group by right-clicking it and selecting 'Add Macro'.
                - Drag and drop the macro so it is above 'Spirnt (PvP)'.
                - Open the new macro by clicking it
                - Rename the macro to `Sprint (Island Sanctuary)` and pick a good icon
                - In the body of the macro type /ac "Duty Action I"
                - Open the 'Link' panel and select the same macro slot we used for 'Sprint (Default)' and 'Sprint (PvP)'
                - Open the 'Link Conditions' panel and untick 'Always Linked'
                - Click the '+' button to add an OR group
                - Add a Location condition by clicking the Hamburger button > Add Condition > Location
                - Click the button after 'Location' and search for 'Unnamed Island (1055)' in the left column, don't click anything in the right column.

                That's it! Now you should have a macro that will switch between Default / PvP / Island Sanctuary based on your location.
                """);
        }
    }
}
