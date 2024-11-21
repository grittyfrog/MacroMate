using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;
using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Conditions;

public record class ContentCondition(
    ExcelId<ContentFinderCondition> content  // i.e. "The Navel (Hard)", "Sycrus Tower"
) : IValueCondition {
    public string ValueName => content.DisplayName();
    public string NarrowName => content.DisplayName();

    /// Default: the Thousand Maws of Toto Rak
    public ContentCondition() : this(1) {}
    public ContentCondition(uint contentId) : this(new ExcelId<ContentFinderCondition>(contentId)) {}

    public bool SatisfiedBy(ICondition other) {
        return this.Equals(other);
    }

    public static ContentCondition? Current() {
        return Env.PlayerLocationManager.Content
            ?.Let(content => new ContentCondition(content));
    }

    public static IValueCondition.IFactory Factory = new ConditionFactory();
    public IValueCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "Content";
        public string ExpressionName => "Content";

        public IValueCondition? Current() => ContentCondition.Current();
        public IValueCondition? FromConditions(CurrentConditions conditions) => conditions.content;
        public IValueCondition Default() => new ContentCondition();

        public IEnumerable<IValueCondition> TopLevel() {
            var excludedContentTypes = new List<uint>() {
                1, // Duty Roulette
                7, // Quest Battles
                19, // Gold Saucer
                20, // Hall of the novice? Things like "Defeat an occupied Target!"
            };
            return Env.DataManager.GetExcelSheet<ContentFinderCondition>()!
                .Where(cfc => cfc.Name != "" && cfc.ContentType.RowId != 0 && !excludedContentTypes.Contains(cfc.ContentType.RowId))
                .Select(cfc => new ContentCondition(cfc.RowId) as IValueCondition);
        }
    }
}
