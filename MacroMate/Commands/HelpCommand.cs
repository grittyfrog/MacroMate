using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text;

namespace MacroMate.Commands;

public class HelpCommand : ICommand {
    public override string CommandName => "help";
    public override string HelpMessage => "Print the help text";

    public override void Execute(List<string> args) {
        var headerLines = new List<string> {
            "Usage: /macromate <command>",
            "",
            "Commands:"
        };
        var subcommandLines = Env.PluginCommandManager.Subcommands.Values
            .Select((command) => $"{command.CommandName}: {command.HelpMessage}");
        var lines = headerLines.Concat(subcommandLines);

        foreach (var line in lines) {
            Env.ChatGui.Print(new XivChatEntry { Message = line });
        }
    }
}
