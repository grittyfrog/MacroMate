using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;

namespace MacroMate.Commands;

/**
 * Manages the commands introduced by this plugin
 */
public class PluginCommandManager : IDisposable {
    private const string CommandName = "/macromate";

    public readonly IReadOnlyDictionary<string, ICommand> Subcommands = new List<ICommand> {
        new ConfigCommand(),
        new ConditionsCommand(),
        new HelpCommand(),
        new ReloadCommand()
    }.ToDictionary(command => command.CommandName);

    public PluginCommandManager() {
        // These `AddHandler` calls don't cause anything to happen when the command is executed.
        // This is because Dalamud can't dispatch commands like "/macromate help" or any command with a space in it.
        // We only make these calls to make the command appear in the "help" section of the plugin.
        foreach(var command in Subcommands.Values) {
            Env.CommandManager.AddHandler(FullCommandName(command), new CommandInfo((_, _) => {}) {
                HelpMessage = command.HelpMessage
            });
        }

        // This `AddHandler` call actually does all the work.
        Env.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) {});
    }

    public void RunCommand(string command, List<string>? arguments = null) {
        arguments ??= new List<string>();
        Subcommands[command].Execute(arguments);
    }

    private void OnCommand(string command, string arguments) {
        var splitArgs = arguments.Split(" ").Where(arg => arg != "").ToList();
        if (splitArgs.Count == 0) {
            Subcommands["config"].Execute(new());
            return;
        }

        var subcommandName = splitArgs[0];
        var subcommand = Subcommands.GetValueOrDefault(subcommandName);
        if (subcommand != null) {
            var remainingArgs = splitArgs.Skip(1).ToList();
            subcommand.Execute(remainingArgs);
        } else {
            Env.ChatGui.PrintError($"Unrecognised subcommand: '{subcommandName}'");
            Subcommands["help"].Execute(new());
        }
    }

    public void Dispose() {
        foreach(var command in Subcommands.Values) {
            Env.CommandManager.RemoveHandler(FullCommandName(command));
        }
        Env.CommandManager.RemoveHandler(CommandName);
    }

    private string FullCommandName(ICommand command) => $"{CommandName} {command.CommandName}";
}
