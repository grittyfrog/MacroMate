using System.Collections.Generic;

namespace MacroMate.Commands;

public abstract class ICommand {
    public abstract string CommandName { get; }
    public abstract string HelpMessage { get; }

    public abstract void Execute(List<string> args);
}
