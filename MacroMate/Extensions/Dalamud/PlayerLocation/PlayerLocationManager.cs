using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.Dotnet;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace MacroMate.Extensions.Dalamud.PlayerLocation;

/**
 * Manages information about the players current location
 *
 * Region/SubArea code adapted from https://github.com/cassandra308/WhereAmIAgain/blob/main/WhereAmIAgain
 */
public unsafe class PlayerLocationManager {
    public ExcelId<ContentFinderCondition>? Content {
        get {
            var territoryTypeFound = Env.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(Env.ClientState.TerritoryType, out var territoryType);
            if (territoryTypeFound) {
                var cfc = new ExcelId<ContentFinderCondition>(territoryType.ContentFinderCondition.RowId);
                return cfc.DefaultIf(x => x.Id == 0);
            }

            return null;
        }
    }

    public ExcelId<PlaceName>? SubAreaName => new ExcelId<PlaceName>(TerritoryInfo.Instance()->SubAreaPlaceNameId).DefaultIf(name => name.Id == 0);

    public ExcelId<PlaceName>? RegionName => new ExcelId<PlaceName>(TerritoryInfo.Instance()->AreaPlaceNameId).DefaultIf(name => name.Id == 0);

    public ExcelId<PlaceName>? TerritoryName {
        get {
            if (Env.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(Env.ClientState.TerritoryType, out var territoryType)) {
                return new ExcelId<PlaceName>(territoryType.PlaceName.RowId);
            }

            return null;
        }
    }
}
