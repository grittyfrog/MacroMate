using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.Sheets;
using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.Lumina;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace MacroMate.Conditions;

public record class WeatherCondition(
    ExcelId<Weather> Weather
) : IValueCondition {
    public string ValueName => Weather.DisplayName();
    public string NarrowName => Weather.DisplayName();

    public bool SatisfiedBy(ICondition other) {
        if (other is WeatherCondition otherWeather) {
            // Compare by weather name instead of exact ID to handle duplicates
            return Weather.Name() == otherWeather.Weather.Name();
        }
        return false;
    }

    public WeatherCondition() : this(1) {}
    public WeatherCondition(uint weatherId) : this(new ExcelId<Weather>(weatherId)) {}

    public static IValueCondition.IFactory Factory => new ConditionFactory();
    public IValueCondition.IFactory FactoryRef => Factory;

    public static WeatherCondition? Current() {
        unsafe {
            var weatherManager = WeatherManager.Instance();
            if (weatherManager == null) { return null; }

            var currentWeatherId = weatherManager->WeatherId;
            if (currentWeatherId == 0) { return null; }

            return new WeatherCondition(currentWeatherId);
        }
    }

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "Weather";
        public string ExpressionName => "Weather";

        public IValueCondition? Current() => WeatherCondition.Current();
        public IValueCondition Default() => new WeatherCondition();
        public IValueCondition? FromConditions(CurrentConditions conditions) => conditions.weather;

        public IEnumerable<IValueCondition> TopLevel() {
            return Env.DataManager.GetExcelSheet<Weather>()!
                .Where(weather => weather.RowId != 0 && !string.IsNullOrEmpty(weather.Name.ExtractText()))
                .GroupBy(weather => weather.Name.ExtractText())
                .Select(group => new WeatherCondition(group.First().RowId) as IValueCondition);
        }
    }
}