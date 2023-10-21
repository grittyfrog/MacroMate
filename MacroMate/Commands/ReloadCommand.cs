using System.Collections.Generic;
using Dalamud.Game.Text;

namespace MacroMate.Commands;

public class ReloadCommand : ICommand {
    public override string CommandName => "reload";
    public override string HelpMessage => "Reload the macros from file";

    public override void Execute(List<string> args) {
        Env.MacroMate.LoadConfig();
        Env.ChatGui.Print(new XivChatEntry {
            Message = "Macro Mate reloaded"
        });
    }
}
