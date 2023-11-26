using System.Collections.Generic;

namespace MacroMate.Conditions;

public interface ICondition {
    string ConditionName => FactoryRef.ConditionName;

    /// The full value name of this condition
    string ValueName { get; }

    /// The "Narrow" name of this condition, typically only includes the name of the most
    /// "inner" part
    string NarrowName { get; }

    IFactory FactoryRef { get; }

    /// True if this condition is considered satisfied if `other` is also satisfied.
    bool SatisfiedBy(ICondition other);

    bool SatisfiedBy(CurrentConditions conditionSet) {
        var condition = FactoryRef.FromConditions(conditionSet);
        if (condition == null) { return false; }
        return SatisfiedBy(condition);
    }

    public interface IFactory {
        string ConditionName { get; }

        /// Returns the current condition for this type.
        ICondition? Current();

        /// Returns a default valid condition
        ICondition Default();

        /// Returns this condition from a ConditionSet
        ICondition? FromConditions(CurrentConditions conditions);

        /// Returns all the possible context-less conditions for this type.
        ///
        /// Deeper context-dependent conditions should use a value produced by this function and
        /// pass it to `Narrow`.
        IEnumerable<ICondition> TopLevel();

        /// Given a chosen condition, offer more "specific" options that are a subset of the
        /// given condition.
        ///
        /// For example: Given a Territory, produce conditions that are regions or sub-areas in that territory.
        IEnumerable<ICondition> Narrow(ICondition search) => new List<ICondition>();

        static IEnumerable<IFactory> All => new IFactory[] {
            ContentCondition.Factory,
            JobCondition.Factory,
            LocationCondition.Factory,
            PvpStateCondition.Factory,
            TargetNameCondition.Factory
        };
    }
}
