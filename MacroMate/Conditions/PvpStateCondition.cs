
using System;
using System.Collections.Generic;

namespace MacroMate.Conditions;

public record class PvpStateCondition(PvpStateCondition.State state) : ICondition {
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

    public ICondition.IFactory FactoryRef => throw new System.NotImplementedException();

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

    public static ICondition.IFactory Factory = new ConditionFactory();
    ICondition.IFactory ICondition.FactoryRef => Factory;

    class ConditionFactory : ICondition.IFactory {
        public string ConditionName => "PvP State";

        public ICondition? Current() => PvpStateCondition.Current();
        public ICondition Default() => new PvpStateCondition();
        public ICondition? FromConditions(CurrentConditions conditions) => conditions.pvpState;
        public IEnumerable<ICondition> TopLevel() {
            foreach (State state in Enum.GetValues(typeof(State))) {
                yield return new PvpStateCondition(state);
            }
        }
    }
}
