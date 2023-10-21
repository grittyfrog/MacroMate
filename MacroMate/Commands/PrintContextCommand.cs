using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text;

namespace MacroMate.Commands;

public class ConditionsCommand : ICommand {
    public override string CommandName => "conditions";
    public override string HelpMessage => "Echo the current conditions";

    public override void Execute(List<string> args) {
        var conditions = Env.ConditionManager.CurrentConditions();

        var lines = conditions.Enumerate()
            .Select(condition => $"{condition.ConditionName}: {condition.ValueName}");

        foreach (var line in lines) {
            Env.ChatGui.Print(new XivChatEntry { Message = line });
        }
    }
}
