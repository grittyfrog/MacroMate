namespace MacroMate.Conditions;

public record class CurrentCraftMaxDurabilityCondition(
    int MaxDurability
) : INumericCondition {
    public static CurrentCraftMaxDurabilityCondition? Current() {
        return new CurrentCraftMaxDurabilityCondition(10);
    }

    public int AsNumber() => MaxDurability;

    public static INumericCondition.IFactory Factory => new ConditionFactory();
    public INumericCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : INumericCondition.IFactory {
        public string ConditionName => "Current Craft - Max Durability";

        public INumericCondition? FromConditions(CurrentConditions conditions) =>
            conditions.craftMaxDurabilityCondition;
        public INumericCondition FromNumber(int num) =>
            new CurrentCraftMaxDurabilityCondition(num);
        public INumericCondition? Current() => CurrentCraftMaxDurabilityCondition.Current();
    }
}
