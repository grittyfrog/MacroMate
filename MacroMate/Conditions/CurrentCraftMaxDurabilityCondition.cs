namespace MacroMate.Conditions;

public record class CurrentCraftMaxDurabilityCondition(
    int MaxDurability
) : INumericCondition {
    public static CurrentCraftMaxDurabilityCondition? Current() {
        var durability = Env.SynthesisManager.CurrentCraftMaxDurability;
        if (durability.HasValue) {
            return new CurrentCraftMaxDurabilityCondition((int)(durability.Value));
        } else {
            return null;
        }
    }

    public int AsNumber() => MaxDurability;

    public static INumericCondition.IFactory Factory => new ConditionFactory();
    public INumericCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : INumericCondition.IFactory {
        public string ConditionName => "Current Craft - Max Durability";
        public string ExpressionName => "CurrentCraft.MaxDurability";

        public INumericCondition? FromConditions(CurrentConditions conditions) =>
            conditions.craftMaxDurabilityCondition;
        public INumericCondition FromNumber(int num) =>
            new CurrentCraftMaxDurabilityCondition(num);
        public INumericCondition? Current() => CurrentCraftMaxDurabilityCondition.Current();
    }
}
