using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Generator.Equals;
using MacroMate.Extensions.Dalamud.Game.ClientState.Conditions;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Conditions;

[Equatable(Explicit = true)]
public partial record class PlayerConditionCondition(
    [property: SetEquality] HashSet<ConditionFlag> Conditions
) : IValueCondition {
    private static Dictionary<ConditionFlag, ConditionFlag> EquivalentFlags = new() {
        { ConditionFlag.BetweenAreas51,                ConditionFlag.BetweenAreas },
        { ConditionFlag.BoundByDuty56,                 ConditionFlag.BoundByDuty },
        { ConditionFlag.BoundByDuty95,                 ConditionFlag.BoundByDuty },
        { ConditionFlag.Casting87,                     ConditionFlag.Casting },
        { ConditionFlag.Jumping61,                     ConditionFlag.Jumping },
        { ConditionFlag.Mounting71,                    ConditionFlag.Mounting },
        { ConditionFlag.Occupied30,                    ConditionFlag.Occupied },
        { ConditionFlag.Occupied33,                    ConditionFlag.Occupied },
        { ConditionFlag.Occupied38,                    ConditionFlag.Occupied },
        { ConditionFlag.Occupied39,                    ConditionFlag.Occupied },
        { ConditionFlag.SufferingStatusAffliction2,    ConditionFlag.SufferingStatusAffliction },
        { ConditionFlag.SufferingStatusAffliction63,   ConditionFlag.SufferingStatusAffliction },
        { ConditionFlag.SufferingStatusAffliction72,   ConditionFlag.SufferingStatusAffliction },
        { ConditionFlag.SufferingStatusAffliction73,   ConditionFlag.SufferingStatusAffliction },
        { ConditionFlag.WatchingCutscene78,            ConditionFlag.WatchingCutscene },
    };

    private static List<ConditionFlag> IgnoredFlags = new() {
        ConditionFlag.None,
        ConditionFlag.MountOrOrnamentTransition,
        ConditionFlag.Unknown96,
        ConditionFlag.Unknown99,
    };

    private static IEnumerable<ConditionFlag> ApplyFlagEquivalence(IEnumerable<ConditionFlag> flags) {
        return flags
            .Where(flag => !IgnoredFlags.Contains(flag))
            .Select(value => {
                if (EquivalentFlags.TryGetValue(value, out var mappedValue)) {
                    return mappedValue;
                } else {
                    return value;
                }
            })
            .Distinct();
    }

    public string ValueName {
        get {
            if (Conditions.Count == 0) { return "<no conditions>"; }
            if (Conditions.Count == 1) { return Conditions.First().Name(); }
            return string.Join("\n", Conditions);
        }
    }

    public string NarrowName => ValueName;

    public PlayerConditionCondition() : this(new HashSet<ConditionFlag> { ConditionFlag.NormalConditions }) {}

    public static PlayerConditionCondition Current() {
        var activeConditions = Enum.GetValues<ConditionFlag>()
            .Where(flag => Env.PlayerCondition[flag])
            .Let(flags => ApplyFlagEquivalence(flags))
            .ToHashSet();
        return new PlayerConditionCondition(activeConditions);
    }

    public bool SatisfiedBy(ICondition other) {
        var otherPlayerCondition = other as PlayerConditionCondition;
        if (otherPlayerCondition == null) { return false; }

        // We are satisfied if all of our conditions are active in [other]
        return Conditions.All(condition => otherPlayerCondition.Conditions.Contains(condition));
    }

    public static IValueCondition.IFactory Factory = new ConditionFactory();
    public IValueCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "Player Condition";
        public string ExpressionName => "PlayerCondition";

        public IValueCondition? Current() => PlayerConditionCondition.Current();
        public IValueCondition? BestInitialValue() {
            var current = PlayerConditionCondition.Current();
            return new PlayerConditionCondition(current.Conditions.Take(1).ToHashSet());
        }
        public IValueCondition Default() => new PlayerConditionCondition();
        public IValueCondition? FromConditions(CurrentConditions conditions) => conditions.playerCondition;
        public IEnumerable<IValueCondition> TopLevel() {
            return Enum.GetValues<ConditionFlag>()
                .Let(flags => ApplyFlagEquivalence(flags))
                .OrderBy(flag => flag.Name())
                .Select(flag => new PlayerConditionCondition(new HashSet<ConditionFlag>() { flag }));
        }
    }
}
