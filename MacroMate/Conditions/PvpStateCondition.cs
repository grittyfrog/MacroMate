
using System;
using System.Collections.Generic;

namespace MacroMate.Conditions;

public record class PvpStateCondition(PvpStateCondition.State state) : IValueCondition {
    public enum State {
        IN_PVP,
        IN_PVP_NO_WOLVES_DEN,
        NOT_IN_PVP
    }

    public string ValueName => state switch {
        State.IN_PVP => "In PvP",
        State.IN_PVP_NO_WOLVES_DEN => "In PvP (exclude Wolves' Den)",
        State.NOT_IN_PVP => "Not in PvP",
        _ => "???"
    };
    public string NarrowName => ValueName;

    public PvpStateCondition() : this(State.IN_PVP) {}

    public bool SatisfiedBy(ICondition other) {
        var otherPvpState = other as PvpStateCondition;
        if (otherPvpState == null) { return false; }

        // IN_PVP_NO_WOLVES_DEN satisfies both itself and IN_PVP.
        if (this.state == State.IN_PVP) {
            return otherPvpState.state == State.IN_PVP || otherPvpState.state == State.IN_PVP_NO_WOLVES_DEN;
        }

        return this.state == otherPvpState.state;
    }

    public static PvpStateCondition Current() {
        if (Env.ClientState.IsPvPExcludingDen) { return new PvpStateCondition(State.IN_PVP_NO_WOLVES_DEN); }
        if (Env.ClientState.IsPvP) { return new PvpStateCondition(State.IN_PVP); }
        return new PvpStateCondition(State.NOT_IN_PVP);
    }

    public static IValueCondition.IFactory Factory = new ConditionFactory();
    public IValueCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "PvP State";

        public IValueCondition? Current() => PvpStateCondition.Current();
        public IValueCondition Default() => new PvpStateCondition();
        public IValueCondition? FromConditions(CurrentConditions conditions) => conditions.pvpState;
        public IEnumerable<IValueCondition> TopLevel() {
            foreach (State state in Enum.GetValues(typeof(State))) {
                yield return new PvpStateCondition(state);
            }
        }
    }
}
