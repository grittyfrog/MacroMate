using MacroMate.Extensions.Lumina;
using Dalamud;
using Dalamud.Game;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Text;
using Dalamud.Game.ClientState.Objects.Enums;

namespace MacroMate.Extensions.Dalamaud.Excel;

public interface ExcelId {
    public uint Id { get; }
    public string? Name();
    public string DisplayName();
}

/// <summary>
/// This object resolves a rowID within an Excel sheet.
///
/// Modified version of https://github.com/goatcorp/Dalamud/blob/04fb5e01d13f3392faf9e731496aff9a175fee37/Dalamud/Game/ClientState/Resolvers/ExcelResolver%7BT%7D.cs#L10
/// </summary>
/// <typeparam name="T">The type of Lumina sheet to resolve.</typeparam>
public record class ExcelId<T>(uint Id) : ExcelId where T : struct, IExcelRow<T> {
    public ExcelId() : this(0) {}

    /// <summary>
    /// Gets GameData linked to this excel row.
    /// </summary>
    public T? GameData => Env.DataManager.GetExcelSheet<T>()?.GetRow(this.Id);

    /// <summary>
    /// Gets GameData linked to this excel row with the specified language.
    /// </summary>
    /// <param name="language">The language.</param>
    /// <returns>The ExcelRow in the specified language.</returns>
    public T? GetWithLanguage(ClientLanguage language) => Env.DataManager.GetExcelSheet<T>(language)?.GetRow(this.Id);

    public string Name() {
        var gameData = GameData;
        if (gameData is BNpcName bNpcName) { return bNpcName.Singular.ExtractText(); }
        if (gameData is ENpcResident eNpc) { return eNpc.Singular.ExtractText(); }
        if (gameData is ContentFinderCondition cfc) { return cfc.Name.ExtractText(); }
        if (gameData is TerritoryType tt) {
            if (tt.PlaceName.Value is {} pn) { return pn.Name.ExtractText(); }
        }
        if (gameData is PlaceName placeName) { return placeName.Name.ExtractText(); }
        if (gameData is ClassJob job) { return job.Abbreviation.ExtractText(); }
        if (gameData is HousingFurniture hf) {
            if (hf.Item.Value is {} hfItem) { return hfItem.Name.ExtractText(); }
        }

        return $"<unknown>";
    }

    public ObjectKind? ObjKind() {
        var gameData = GameData;
        if (gameData is BNpcName) { return ObjectKind.BattleNpc; }
        if (gameData is ENpcResident) { return ObjectKind.EventNpc; }
        if (gameData is HousingFurniture) { return ObjectKind.Housing; }

        return null;
    }

    public string DisplayName() {
        var sb = new StringBuilder();
        sb.Append(Name());
        sb.Append($" ({Id})");
        if (ObjKind() is {} objKind) {
            sb.Append($" [{objKind}]");
        }
        return sb.ToString();
    }

    public override string ToString() => $"ExcelId({Id})";
}
