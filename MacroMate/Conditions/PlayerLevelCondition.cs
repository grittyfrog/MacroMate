using System;

namespace MacroMate.Conditions;

public record class PlayerLevelCondition(
    int Level
) : INumericCondition
{
    public static PlayerLevelCondition? Current()
    {
        try
        {
            if (!Env.PlayerState.IsLoaded) { return null; }

            var player = Env.ObjectTable.LocalPlayer;
            if (player == null) { return null; }

            var level = player.Level;

            return new PlayerLevelCondition(level);
        }
        catch (Exception)
        {
            return null;
        }
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
