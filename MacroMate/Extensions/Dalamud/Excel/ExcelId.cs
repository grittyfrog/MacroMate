using MacroMate.Extensions.Lumina;
using Dalamud;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace MacroMate.Extensions.Dalamaud.Excel;

public interface ExcelId {
    public uint Id { get; }
    public string DisplayName();
}

/// <summary>
/// This object resolves a rowID within an Excel sheet.
///
/// Modified version of https://github.com/goatcorp/Dalamud/blob/04fb5e01d13f3392faf9e731496aff9a175fee37/Dalamud/Game/ClientState/Resolvers/ExcelResolver%7BT%7D.cs#L10
/// </summary>
/// <typeparam name="T">The type of Lumina sheet to resolve.</typeparam>
public record class ExcelId<T>(uint Id) : ExcelId where T : ExcelRow {
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

    public string? Name() {
        if (GameData is BNpcName bNpcName) { return bNpcName.Singular.Text(); }
        if (GameData is ENpcResident eNpc) { return eNpc.Singular.Text(); }
        if (GameData is ContentFinderCondition cfc) { return cfc.Name.Text(); }
        if (GameData is TerritoryType tt) {
            var pn = tt.PlaceName.Value;
            if (pn != null) { return pn.Name.Text(); }
        }
        if (GameData is PlaceName placeName) { return placeName.Name.Text(); }
        if (GameData is ClassJob job) { return job.Abbreviation.Text(); }

        return null;
    }

    public string DisplayName() {
        if (Name() is {} name) {
            return $"{name} ({Id})";
        } else {
            return $"<id:{Id}>";
        }
    }

    public override string ToString() => $"ExcelId({Id})";
}
