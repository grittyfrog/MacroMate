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

        // We check if the names are equal by their string representation instead of their ID.
        // We do this because some areas have many ids mapping to the same name, and we only really care
        // that the name is the same (since we don't want a giant location list filled with 100's of "Mist" entries)

        bool territoryEqual = this.territory.Name().Equals(otherLocation.territory.Name());
        if (!territoryEqual) {
            return false;
        }

        // If regionOrSubAreaName is null, assume we are satisfied (since we know the territory is equal)
        if (regionOrSubAreaName == null) { return true; }

        bool regionOrSubAreaEqual = this.regionOrSubAreaName.Name().Equals(otherLocation.regionOrSubAreaName?.Name());
        return regionOrSubAreaEqual;
    }

    public static ICondition.IFactory Factory = new ConditionFactory();
    ICondition.IFactory ICondition.FactoryRef => Factory;

    class ConditionFactory : ICondition.IFactory {
        public string ConditionName => "Location";
        public ICondition? Current() => LocationCondition.Current();
        public ICondition Default() => new LocationCondition();
        public ICondition? FromConditions(CurrentConditions conditions) => conditions.location;

        public IEnumerable<ICondition> TopLevel() {
            return Env.DataManager.GetExcelSheet<TerritoryType>()!
                .Where(territoryType => territoryType.PlaceName.Row != 0)
                .DistinctBy(territoryType => territoryType.PlaceName.Row)
                .Select(territoryType =>
                    new LocationCondition(territoryId: territoryType.RowId) as ICondition
                );
        }

        public IEnumerable<ICondition> Narrow(ICondition search) {
            // We can only narrow conditions of our type
            var locationCondition = search as LocationCondition;
            if (locationCondition == null) { return new List<ICondition>(); }

            // If we already have a regionOrSubAreaNameId then we can't do any further narrowing.
            if (locationCondition.regionOrSubAreaName != null) { return new List<ICondition>(); }

            // Otherwise, we can fill out the data using Map Markers!
            //
            // First, a territory might have multiple maps (i.e. multiple floors), so we need all of them
            var locationMaps = Env.DataManager.GetExcelSheet<Map>()!
                .Where(map => map.TerritoryType.Row == locationCondition.territory.Id);
            var locationMapMarkerRanges = locationMaps.Select(map => (uint)map.MapMarkerRange).ToHashSet();

            // Now for each of the maps we want to pull all the named sub-locations that are part of
            // that map
            return Env.DataManager.GetExcelSheet<MapMarker>()!
                .Where(mapMarker =>
                    locationMapMarkerRanges.Contains(mapMarker.RowId) && mapMarker.PlaceNameSubtext.Row != 0
                )
                .DistinctBy(mapMarker => mapMarker.PlaceNameSubtext.Row)
                .Select(mapMarker =>
                    locationCondition with { regionOrSubAreaName = new ExcelId<PlaceName>(mapMarker.PlaceNameSubtext.Row) } as ICondition
                );
        }
    }
}
