using System.Collections.Generic;
using System.Linq;
using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.Dotnet;
using Dalamud.Game.ClientState.Objects.Types;
using Lumina.Excel.GeneratedSheets;

namespace MacroMate.Conditions;

public record class TargetNpcCondition(
    ExcelId<BNpcName> targetName
) : ICondition {
    string ICondition.ValueName => targetName.DisplayName();
    string ICondition.NarrowName => targetName.DisplayName();

    bool ICondition.SatisfiedBy(ICondition other) => this.Equals(other);

    /// Default: ruins runner
    public TargetNpcCondition() : this(2) {}
    public TargetNpcCondition(uint targetNameId) : this(new ExcelId<BNpcName>(targetNameId)) {}

    public static TargetNpcCondition? Current() {
        var target = Env.TargetManager.Target;
        var targetNameId = (target as Character)?.NameId;
        return targetNameId
            ?.Let(id => new ExcelId<BNpcName>(id))
            ?.DefaultIf(i => i.Id == 0)
            ?.Let(excelId => new TargetNpcCondition(excelId));
    }

    public static ICondition.IFactory Factory = new ConditionFactory();
    ICondition.IFactory ICondition.FactoryRef => Factory;

    class ConditionFactory : ICondition.IFactory {
        public string ConditionName => "Target NPC";
        public ICondition? Current() => TargetNpcCondition.Current();
        public ICondition Default() => new TargetNpcCondition();
        public ICondition? FromConditions(CurrentConditions conditions) => conditions.targetNpc;

        public List<ICondition> TopLevel() {
            return Env.DataManager.GetExcelSheet<BNpcName>()!
                .Where(npcName => npcName.Singular != "")
                .Select(npcName => new TargetNpcCondition(npcName.RowId) as ICondition)
                .ToList();
        }
    }
}
