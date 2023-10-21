using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.Dotnet;
using Dalamud.Utility.Signatures;
using Lumina.Excel.GeneratedSheets;

namespace MacroMate.Extensions.Dalamud.PlayerLocation;

/**
 * Manages information about the players current location
 *
 * Region/SubArea code adapted from https://github.com/cassandra308/WhereAmIAgain/blob/main/WhereAmIAgain
 */
public unsafe class PlayerLocationManager {
    [Signature("8B 2D ?? ?? ?? ?? 41 BF", ScanType = ScanType.StaticAddress)]
    private readonly TerritoryInfoStruct* territoryInfo = null!;

    public PlayerLocationManager() {
        Env.GameInteropProvider.InitializeFromAttributes(this);
    }

    public ExcelId<ContentFinderCondition>? Content {
        get {
            var territoryType = Env.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(Env.ClientState.TerritoryType);
            return territoryType
                ?.Let(type => new ExcelId<ContentFinderCondition>(type.ContentFinderCondition.Row))
                ?.DefaultIf(cfc => cfc.Id == 0);
        }
    }

    public ExcelId<PlaceName>? SubAreaName => new ExcelId<PlaceName>(territoryInfo->SubAreaID).DefaultIf(name => name.Id == 0);

    public ExcelId<PlaceName>? RegionName => new ExcelId<PlaceName>(territoryInfo->RegionID).DefaultIf(name => name.Id == 0);

    public ExcelId<PlaceName>? TerritoryName {
        get {
            var territoryType = Env.DataManager.GetExcelSheet<TerritoryType>()!.GetRow(Env.ClientState.TerritoryType);
            return territoryType?.Let(type => new ExcelId<PlaceName>(type.PlaceName.Row));
        }
    }
}
