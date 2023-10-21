using System.Collections.Generic;
using System.Linq;
using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.Dotnet;
using Lumina.Excel.GeneratedSheets;

namespace MacroMate.Conditions;

public record class ContentCondition(
    ExcelId<ContentFinderCondition> content  // i.e. "The Navel (Hard)", "Sycrus Tower"
) : ICondition {
    string ICondition.ValueName => content.DisplayName();
    string ICondition.NarrowName => content.DisplayName();

    /// Default: the Thousand Maws of Toto Rak
    public ContentCondition() : this(1) {}
    public ContentCondition(uint contentId) : this(new ExcelId<ContentFinderCondition>(contentId)) {}

    bool ICondition.SatisfiedBy(ICondition other) => this.Equals(other);

    public static ContentCondition? Current() {
        return Env.PlayerLocationManager.Content
            ?.Let(content => new ContentCondition(content));
    }

    public static ICondition.IFactory Factory = new ConditionFactory();
    ICondition.IFactory ICondition.FactoryRef => Factory;

    class ConditionFactory : ICondition.IFactory {
        public string ConditionName => "Content";
        public ICondition? Current() => ContentCondition.Current();
        public ICondition? FromConditions(CurrentConditions conditions) => conditions.content;
        public ICondition Default() => new ContentCondition();

        public List<ICondition> TopLevel() {
            var excludedContentTypes = new List<uint>() {
                1, // Duty Roulette
                7, // Quest Battles
                19, // Gold Saucer
                20, // Hall of the novice? Things like "Defeat an occupied Target!"
            };
            return Env.DataManager.GetExcelSheet<ContentFinderCondition>()!
                .Where(cfc => cfc.Name != "" && cfc.ContentType.Row != 0 && !excludedContentTypes.Contains(cfc.ContentType.Row))
                .Select(cfc => new ContentCondition(cfc.RowId) as ICondition)
                .ToList();
        }
    }
}
