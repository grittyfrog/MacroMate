using System;
using Dalamud.Game.Command;

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
        Env.PluginWindowManager.MainWindow.Toggle();
    }

    public void Dispose() {
        Env.CommandManager.RemoveHandler("/macromate");
        Env.CommandManager.RemoveHandler("/mm");
    }
}
