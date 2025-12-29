namespace MacroMate.Conditions;

public record class PlayerLevelCondition(
    int Level
) : INumericCondition {
    public static PlayerLevelCondition? Current() {
        if (!Env.PlayerState.IsLoaded) { return null; }

        return new PlayerLevelCondition(Env.PlayerState.Level);
    }

    public int AsNumber() => Level;

    public static INumericCondition.IFactory Factory => new ConditionFactory();
    public INumericCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : INumericCondition.IFactory {
        public string ConditionName => "Player Level";
        public string ExpressionName => "Player.Level";

        public INumericCondition? FromConditions(CurrentConditions conditions) =>
            conditions.playerLevelCondition;
        public INumericCondition FromNumber(int num) =>
            new PlayerLevelCondition(num);
        public INumericCondition? Current() => PlayerLevelCondition.Current();
    }
}
