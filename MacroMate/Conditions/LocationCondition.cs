using System;
using System.Collections.Generic;
using System.Linq;
using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.Dotnet;
using Lumina.Excel.GeneratedSheets;

namespace MacroMate.Conditions;

public record class LocationCondition(
    ExcelId<TerritoryType> territory,          // i.e. "Limsa Lominsa Lower Decks"
    ExcelId<PlaceName>? regionOrSubAreaName    // i.e. "The Octant" or "Seasong Grotto"
) : ICondition {
    string ICondition.ValueName {
        get {
            var names = new List<string?>() {
                territory.DisplayName(),
                regionOrSubAreaName?.DisplayName()
            };
            return String.Join(", ", names.WithoutNull());
        }
    }
    string ICondition.NarrowName => regionOrSubAreaName?.DisplayName() ?? territory.DisplayName();

    /// Default: Limsa Lomina Upper Decks
    public LocationCondition() : this(territoryId: 128) {}

    public LocationCondition(
        uint territoryId,
        uint? regionOrSubAreaNameId = null
    ) : this(
        territory: territoryId.Let(id => new ExcelId<TerritoryType>(id)),
        regionOrSubAreaName: regionOrSubAreaNameId?.Let(id => new ExcelId<PlaceName>(id))
    ) {}

    public static LocationCondition Current() {
        return new LocationCondition(
            territory: new ExcelId<TerritoryType>(Env.ClientState.TerritoryType),
            // We prefer sub-area since it's more specific then region. (A sub-area always exists in a region)
            regionOrSubAreaName: Env.PlayerLocationManager.SubAreaName ?? Env.PlayerLocationManager.RegionName
        );
    }


    bool ICondition.SatisfiedBy(ICondition other) {
        var otherLocation = other as LocationCondition;
        if (otherLocation == null) { return false; }

        // If regionOrSubAreaName is null, assume it is satisfied
        if (regionOrSubAreaName == null) { return this.territory.Equals(otherLocation.territory); }

        // Otherwise, we need the whole thing to be equal
        return this.Equals(otherLocation);
    }

    public static ICondition.IFactory Factory = new ConditionFactory();
    ICondition.IFactory ICondition.FactoryRef => Factory;

    class ConditionFactory : ICondition.IFactory {
        public string ConditionName => "Location";
        public ICondition? Current() => LocationCondition.Current();
        public ICondition Default() => new LocationCondition();
        public ICondition? FromConditions(CurrentConditions conditions) => conditions.location;

        public List<ICondition> TopLevel() {
            return Env.DataManager.GetExcelSheet<TerritoryType>()!
                .Where(territoryType => territoryType.PlaceName.Row != 0)
                .DistinctBy(territoryType => territoryType.PlaceName.Row)
                .Select(territoryType =>
                    new LocationCondition(territoryId: territoryType.RowId) as ICondition
                )
                .ToList();
        }

        public List<ICondition> Narrow(ICondition search) {
            // We can only narrow conditions of our type
            var locationCondition = search as LocationCondition;
            if (locationCondition == null) { return new(); }

            // If we already have a regionOrSubAreaNameId then we can't do any further narrowing.
            if (locationCondition.regionOrSubAreaName != null) { return new(); }

            // Otherwise, we can fill out the data using Map Markers!
            return Env.DataManager.GetExcelSheet<MapMarker>()!
                .Where(mapMarker =>
                    mapMarker.RowId == locationCondition.territory.GameData!.Map.Value!.MapMarkerRange
                       && mapMarker.PlaceNameSubtext.Row != 0
                )
                .DistinctBy(mapMarker => mapMarker.PlaceNameSubtext.Row)
                .Select(mapMarker =>
                    locationCondition with { regionOrSubAreaName = new ExcelId<PlaceName>(mapMarker.PlaceNameSubtext.Row) } as ICondition
                )
                .ToList();
        }
    }
}
