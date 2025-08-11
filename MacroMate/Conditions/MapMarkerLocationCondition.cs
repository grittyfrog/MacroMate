using System.Collections.Generic;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.FFXIVClientStructs;

namespace MacroMate.Conditions;

public record class MapMarkerLocationCondition(ExcelId<TerritoryType> Territory) : IValueCondition {
    public string ValueName => Territory.DisplayName();
    public string NarrowName => ValueName;

    /// Default: Limsa Lomina Upper Decks
    public MapMarkerLocationCondition() : this(128) {}
    public MapMarkerLocationCondition(uint territoryId) : this(new ExcelId<TerritoryType>(territoryId)) {}


    public bool SatisfiedBy(ICondition other) {
        return this.Equals(other);
    }

    public static MapMarkerLocationCondition? Current() {
        uint mapMarkerTerritoryId;
        unsafe {
            var agentMap = XIVCS.GetAgent<AgentMap>();
            if (agentMap->FlagMarkerCount == 0) { return null; }
            mapMarkerTerritoryId = agentMap->FlagMapMarkers[0].TerritoryId;
        }
        return new MapMarkerLocationCondition(mapMarkerTerritoryId);
    }

    public IValueCondition.IFactory FactoryRef => Factory;
    public static IValueCondition.IFactory Factory => new ConditionFactory();

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "Map Flag - Location";
        public string ExpressionName => "MapFlag.Location";

        public IValueCondition? Current() => MapMarkerLocationCondition.Current();
        public IValueCondition Default() => new MapMarkerLocationCondition();
        public IValueCondition? FromConditions(CurrentConditions conditions) {
            return conditions.mapMarkerLocationCondition;
        }

        public IEnumerable<IValueCondition> TopLevel() {
            return Env.DataManager.GetExcelSheet<TerritoryType>()!
                .Where(territoryType => territoryType.PlaceName.RowId != 0)
                .DistinctBy(territoryType => territoryType.PlaceName.RowId)
                .Select(territoryType =>
                    new MapMarkerLocationCondition(territoryId: territoryType.RowId) as IValueCondition
                );
        }
    }
}
