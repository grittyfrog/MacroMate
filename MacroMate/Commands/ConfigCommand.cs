using System.Collections.Generic;

namespace MacroMate.Commands;

public class ConfigCommand : ICommand {
    public override string CommandName => "config";
    public override string HelpMessage => "Show the config window";

    public override void Execute(List<string> args) {
        var mainWindow = Env.PluginWindowManager.MainWindow;
        if (mainWindow != null) {
            mainWindow.IsOpen = true;
        }
    }
}
