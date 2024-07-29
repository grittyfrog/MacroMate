using System;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using MacroMate.MacroTree;

namespace MacroMate.Commands;

/**
 * Manages the commands introduced by this plugin
 */
public class PluginCommandManager : IDisposable {
    public PluginCommandManager() {
        var mainCommandInfo = new CommandInfo(OnMainCommand) { HelpMessage = "Show the Macro Mate window" };
        Env.CommandManager.AddHandler("/macromate", mainCommandInfo);
        Env.CommandManager.AddHandler("/mm", mainCommandInfo);
    }

    private void OnMainCommand(string command, string arguments) {
        if (arguments.Trim() == "") {
            Env.PluginWindowManager.MainWindow.Toggle();
            return;
        }

        var subcommandIndex = arguments.IndexOf(" ");
        var subcommand = subcommandIndex == -1 ? arguments : arguments.Substring(0, subcommandIndex).Trim();
        var subarguments = subcommandIndex == -1 ? null : arguments.Substring(subcommandIndex).Trim();
        switch (subcommand) {
            case "open": OnOpenSubcommand(command, subarguments); break;
            case "help": PrintHelp(); return;
            default:
                Env.ChatGui.PrintError($"unrecognized subcommand: '{subcommand}'");
                return;
        }
    }

    private void PrintHelp() {
        var seStr = new SeStringBuilder()
            .Append("[MacroMate Help]\n")
            .Append("Usage: '/macromate' or '/mm' - toggle the config window.\n")
            .Append("Usage: '/macromate <command>' or '/mm command' - run a subcommand.\n")
            .Append("\n")
            .Append("Subcommands:\n")
            .Append("  open <path> - opens the macro identified by path (see Help for path details)\n")
            .Append("  help - print this message")
            .Build();

        Env.ChatGui.Print(seStr);
    }

    /// Syntax: `/mm open <path>`. Assumes `arguments` is the full path (to allow for spaces)
    private void OnOpenSubcommand(string command, string? path) {
        if (path == null || path == "") {
            Env.ChatGui.PrintError($"Expected {command} open <path> (try {command} help)");
            return;
        }

        try {
            var parsedPath = MacroPath.ParseText(path);
            var target = Env.MacroConfig.Root.Walk(parsedPath);
            if (target == null) { Env.ChatGui.PrintError($"no node found at '{path}'"); return; }
            if (target is MateNode.Macro macroTarget) {
                Env.PluginWindowManager.MacroWindow.ShowOrFocus(macroTarget);
            } else {
                 Env.ChatGui.PrintError($"expected macro at '{path}'");
                 return;
            }
        } catch (ArgumentException ex) {
            Env.ChatGui.PrintError(ex.Message);
        }
    }

    public void Dispose() {
        Env.CommandManager.RemoveHandler("/macromate");
        Env.CommandManager.RemoveHandler("/mm");
    }
}
