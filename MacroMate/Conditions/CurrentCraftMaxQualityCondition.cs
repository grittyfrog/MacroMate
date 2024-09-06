namespace MacroMate.Conditions;

public record class CurrentCraftMaxQualityCondition(
    int MaxQuality
) : INumericCondition {
    public static CurrentCraftMaxQualityCondition? Current() {
        var maxQuality = Env.SynthesisManager.CurrentCraftMaxQuality;
        if (maxQuality.HasValue) {
            return new CurrentCraftMaxQualityCondition((int)(maxQuality.Value));
        } else {
            return null;
        }
    }

    public int AsNumber() => MaxQuality;

    public static INumericCondition.IFactory Factory => new ConditionFactory();
    public INumericCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : INumericCondition.IFactory {
        public string ConditionName => "Current Craft - Max Quality";
        public string ExpressionName => "CurrentCraft.MaxQuality";

        public INumericCondition? FromConditions(CurrentConditions conditions) =>
            conditions.craftMaxQualityCondition;
        public INumericCondition FromNumber(int num) =>
            new CurrentCraftMaxQualityCondition(num);
        public INumericCondition? Current() => CurrentCraftMaxQualityCondition.Current();
    }
}
