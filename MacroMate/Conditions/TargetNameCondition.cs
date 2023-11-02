using System.Collections.Generic;
using System.Linq;
using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.Dotnet;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.GeneratedSheets;
using OneOf;

namespace MacroMate.Conditions;

public record class TargetNameCondition(
    OneOf<ExcelId<BNpcName>, string> targetName
) : ICondition {
    string Name => targetName.Match(
        bNpc => bNpc.DisplayName(),
        customName => $"{customName} (Custom)"
    );
    string ICondition.ValueName => Name;
    string ICondition.NarrowName => Name;

    bool ICondition.SatisfiedBy(ICondition other) => this.Equals(other);

    /// Default: ruins runner
    public TargetNameCondition() : this(new ExcelId<BNpcName>(2)) {}

    public static TargetNameCondition? Current() {
        var target = Env.TargetManager.Target;
        if (target is Character targetCharacter) {
            var targetNameId = targetCharacter?.NameId;
            var targetNameExcelId = targetNameId
                ?.Let(id => new ExcelId<BNpcName>(id))
                ?.DefaultIf(i => i.Id == 0);

            if (targetNameExcelId != null) {
                return new TargetNameCondition(targetNameExcelId);
            }
        }

        if (target != null) {
            var name = target.Name.ToString();
            return new TargetNameCondition(name);
        }

        return null;
    }

    public static ICondition.IFactory Factory = new ConditionFactory();
    ICondition.IFactory ICondition.FactoryRef => Factory;

    class ConditionFactory : ICondition.IFactory {
        public string ConditionName => "Target";
        public ICondition? Current() => TargetNameCondition.Current();
        public ICondition Default() => new TargetNameCondition();
        public ICondition? FromConditions(CurrentConditions conditions) => conditions.targetNpc;

        public List<ICondition> TopLevel() {
            return Env.DataManager.GetExcelSheet<BNpcName>()!
                .Where(npcName => npcName.Singular != "")
                .Select(npcName =>
                    new TargetNameCondition(new ExcelId<BNpcName>(npcName.RowId)) as ICondition
                )
                .ToList();
        }
    }
}
