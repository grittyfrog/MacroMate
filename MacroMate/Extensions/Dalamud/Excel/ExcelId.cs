using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
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

    public string DisplayName() {
        var npcName = (GameData as BNpcName)?.Singular?.Text();
        if (npcName != null) { return $"{npcName} ({Id})"; }

        var enpcName = (GameData as ENpcResident)?.Singular?.Text();
        if (enpcName != null) { return $"{enpcName} ({Id})"; }

        var contentName = (GameData as ContentFinderCondition)?.Name?.Text();
        if (contentName != null) { return $"{contentName} ({Id})"; }

        var territoryType = (GameData as TerritoryType)?.PlaceName?.Value?.Name?.Text();
        if (territoryType != null) { return $"{territoryType} ({Id})"; }

        var placeName = (GameData as PlaceName)?.Name?.Text();
        if (placeName != null) { return $"{placeName} ({Id})"; }

        var jobName = (GameData as ClassJob)?.Abbreviation?.Text();
        if (jobName != null) { return $"{jobName} ({Id})"; }

        return "$<id:{Id}>";
    }

    public override string ToString() => $"ExcelId({Id})";
}
