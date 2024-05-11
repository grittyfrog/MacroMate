using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using MacroMate.Extensions.Dotnet;

namespace MacroMate.Conditions;

public record class PlayerConditionCondition(
    List<ConditionFlag> Conditions
) : ICondition {
    private static Dictionary<ConditionFlag, ConditionFlag> EquivalentFlags = new() {
        { ConditionFlag.BetweenAreas51,                ConditionFlag.BetweenAreas },
        { ConditionFlag.BoundByDuty56,                 ConditionFlag.BoundByDuty },
        { ConditionFlag.BoundByDuty95,                 ConditionFlag.BoundByDuty },
        { ConditionFlag.Casting87,                     ConditionFlag.Casting },
        { ConditionFlag.Crafting40,                    ConditionFlag.Crafting },
        { ConditionFlag.Gathering42,                   ConditionFlag.Gathering },
        { ConditionFlag.InThisState89,                 ConditionFlag.InThisState88 },
        { ConditionFlag.Jumping61,                     ConditionFlag.Jumping },
        { ConditionFlag.Mounting71,                    ConditionFlag.Mounting },
        { ConditionFlag.Mounted2,                      ConditionFlag.Mounted },
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
        ConditionFlag.Unknown57,
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
            if (Conditions.Count == 1) { return Conditions[0].ToString(); }
            return string.Join("\n", Conditions);
        }
    }
    public string NarrowName => ValueName;

    public PlayerConditionCondition() : this(new List<ConditionFlag> { ConditionFlag.NormalConditions }) {}

    public static PlayerConditionCondition Current() {
        var activeConditions = Enum.GetValues<ConditionFlag>()
            .Where(flag => Env.PlayerCondition[flag])
            .Let(flags => ApplyFlagEquivalence(flags))
            .ToList();
        return new PlayerConditionCondition(activeConditions);
    }

    public bool SatisfiedBy(ICondition other) {
        var otherPlayerCondition = other as PlayerConditionCondition;
        if (otherPlayerCondition == null) { return false; }

        // We are satisfied if all of our conditions are active in [other]
        return Conditions.All(condition => otherPlayerCondition.Conditions.Contains(condition));
    }

    public static ICondition.IFactory Factory = new ConditionFactory();
    public ICondition.IFactory FactoryRef => Factory;

    class ConditionFactory : ICondition.IFactory {
        public string ConditionName => "Player Condition";

        public ICondition? Current() => PlayerConditionCondition.Current();
        public ICondition? BestInitialValue() {
            var current = PlayerConditionCondition.Current();
            return new PlayerConditionCondition(current.Conditions.Take(1).ToList());
        }
        public ICondition Default() => new PlayerConditionCondition();
        public ICondition? FromConditions(CurrentConditions conditions) => conditions.playerCondition;
        public IEnumerable<ICondition> TopLevel() {
            return Enum.GetValues<ConditionFlag>()
                .Let(flags => ApplyFlagEquivalence(flags))
                .OrderBy(flag => Enum.GetName(flag))
                .Select(flag => new PlayerConditionCondition(new List<ConditionFlag>() { flag }));
        }
    }
}
