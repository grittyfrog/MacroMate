namespace MacroMate.Conditions;

public record class CurrentCraftDifficultyCondition(
    int Difficulty
) : INumericCondition {
    public static CurrentCraftDifficultyCondition? Current() {
        var difficulty = Env.SynthesisManager.CurrentCraftDifficulty;
        if (difficulty.HasValue) {
            return new CurrentCraftDifficultyCondition((int)(difficulty.Value));
        } else {
            return null;
        }
    }

    public int AsNumber() => Difficulty;

    public static INumericCondition.IFactory Factory => new ConditionFactory();
    public INumericCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : INumericCondition.IFactory {
        public string ConditionName => "Current Craft - Difficulty";
        public string ExpressionName => "CurrentCraft.Difficulty";

        public INumericCondition? FromConditions(CurrentConditions conditions) =>
            conditions.craftDifficultyCondition;
        public INumericCondition FromNumber(int num) =>
            new CurrentCraftDifficultyCondition(num);
        public INumericCondition? Current() => CurrentCraftDifficultyCondition.Current();
    }
}
