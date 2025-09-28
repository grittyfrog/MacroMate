namespace MacroMate.Conditions;

public record class PlayerLevelCondition(
    int Level
) : INumericCondition {
    public static PlayerLevelCondition? Current() {
        var player = Env.ClientState.LocalPlayer;
        if (player == null) { return null; }

        return new PlayerLevelCondition(player.Level);
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
