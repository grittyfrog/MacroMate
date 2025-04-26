using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lumina.Excel.Sheets;
using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Conditions;

public record class WorldCondition(
    ExcelId<World> World,
    bool DataCenterOnly
) : IValueCondition {
    public string ValueName {
        get {
            var sb = new StringBuilder();
            if (World.GameData.HasValue) {
                sb.Append(World.GameData.Value.DataCenter.Value.Name);
                if (!DataCenterOnly) {
                    sb.Append("/");
                    sb.Append(World.GameData.Value.Name);
                }
            }
            return sb.ToString();
        }
    }
    public string NarrowName {
        get {
            if (DataCenterOnly) {
                if (!World.GameData.HasValue) { return "<err:world>"; }
                var world = World.GameData.Value;

                if (!world.DataCenter.ValueNullable.HasValue) { return "<err:dc>"; }
                return world.DataCenter.ValueNullable.Value.Name.ExtractText();
            } else {
                if (!World.GameData.HasValue) { return "<err:world>"; }
                return World.GameData.Value.Name.ExtractText();
            }
        }
    }

    public WorldCondition() : this(21) {} // Ravana, first public
    public WorldCondition(uint id) : this(new ExcelId<World>(id), false) {}

    public static WorldCondition? Current() {
        return Env.PlayerLocationManager.CurrentWorld?.Let(world =>
            new WorldCondition(World: world, DataCenterOnly: false)
        );
    }

    public bool SatisfiedBy(ICondition other) {
        var otherWorld = other as WorldCondition;
        if (otherWorld == null) { return false; }

        if (DataCenterOnly) {
            return this.World.GameData?.DataCenter.RowId == otherWorld.World?.GameData?.DataCenter.RowId;
        }

        return this.World.Id.Equals(otherWorld.World.Id);
    }

    public static IValueCondition.IFactory Factory = new ConditionFactory();
    public IValueCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "World";
        public string ExpressionName => "World";

        public IValueCondition? Current() => WorldCondition.Current();
        public IValueCondition Default() => new WorldCondition();
        public IValueCondition? FromConditions(CurrentConditions conditions) => conditions.world;

        public IEnumerable<IValueCondition> TopLevel() {
            return Env.DataManager.GetExcelSheet<World>()!
                .Where(world => world.RowId != 0 && world.IsPublic)
                .DistinctBy(world => world.DataCenter)
                .Select(world =>
                    new WorldCondition(
                        World: new ExcelId<World>(world.RowId),
                        DataCenterOnly: true
                    ) as IValueCondition
                );
        }

        public IEnumerable<IValueCondition> Narrow(IValueCondition search) {
            // We can only narrow conditions of our type
            var worldCondition = search as WorldCondition;
            if (worldCondition == null) { return new List<IValueCondition>(); }

            if (!worldCondition.DataCenterOnly) { return new List<IValueCondition>(); }

            return Env.DataManager.GetExcelSheet<World>()!
                .Where(world => world.RowId != 0 && world.IsPublic && world.DataCenter.RowId == worldCondition.World.GameData?.DataCenter.RowId)
                .Select(world =>
                    new WorldCondition(
                        World: new ExcelId<World>(world.RowId),
                        DataCenterOnly: false
                    ) as IValueCondition
                );
        }
    }
}
