using System;
using System.Collections.Generic;

namespace MacroMate.Conditions;

public interface ICondition {
    string ConditionName => FactoryRef.ConditionName;

    ICondition.IFactory FactoryRef { get; }

    /// The text representation of this condition's value
    string ValueName { get; }

    /// Wraps this condition in a default operator (for new conditions)
    OpExpr WrapInDefaultOp();

    public interface IFactory {
        string ConditionName { get; }

        ICondition? Current();

        /// Returns the best value to use for new Conditions of this type. Usually `Current`, but can be
        /// overridden
        ICondition? BestInitialValue() => Current();

        /// Returns a default valid condition
        ICondition Default();

        static IEnumerable<IFactory> All => new IFactory[] {
            ContentCondition.Factory,
            JobCondition.Factory,
            LocationCondition.Factory,
            PlayerConditionCondition.Factory,
            PvpStateCondition.Factory,
            TargetNameCondition.Factory,
            CurrentCraftMaxDurabilityCondition.Factory
        };
    }
}

public interface IValueCondition : ICondition {
    /// The "Narrow" name of this condition, typically only includes the name of the most
    /// "inner" part
    string NarrowName { get; }

    /// True if this condition is considered satisfied if `other` is also satisfied.
    bool SatisfiedBy(ICondition other);

    bool SatisfiedBy(CurrentConditions conditionSet) {
        var condition = FactoryRef.FromConditions(conditionSet);
        if (condition == null) { return false; }
        return SatisfiedBy(condition);
    }

    OpExpr ICondition.WrapInDefaultOp() => new OpExpr.Is(this);

    public new IValueCondition.IFactory FactoryRef { get; }
    ICondition.IFactory ICondition.FactoryRef => FactoryRef;

    public new interface IFactory : ICondition.IFactory {
        IValueCondition? FromConditions(CurrentConditions conditions);

        /// Returns the best value to use for new Conditions of this type. Usually `Current`, but can be
        /// overridden
        new IValueCondition? BestInitialValue() => Current();
        ICondition? ICondition.IFactory.BestInitialValue() => BestInitialValue();

        public new IValueCondition? Current();
        ICondition? ICondition.IFactory.Current() => Current();

        public new IValueCondition Default();
        ICondition ICondition.IFactory.Default() => Default();

        /// Returns all the possible context-less conditions for this type.
        ///
        /// Deeper context-dependent conditions should use a value produced by this function and
        /// pass it to `Narrow`.
        IEnumerable<IValueCondition> TopLevel();

        /// Given a chosen condition, offer more "specific" options that are a subset of the
        /// given condition.
        /// For example: Given a Territory, produce conditions that are regions or sub-areas in that territory.
        IEnumerable<IValueCondition> Narrow(IValueCondition search) => new List<IValueCondition>();
    }
}


/// <summary>
/// A condition that can be treated as a number and compared to a particular value.
/// </summary>
public interface INumericCondition : ICondition {
    int AsNumber();

    OpExpr ICondition.WrapInDefaultOp() => new OpExpr.Lte(this);

    public new INumericCondition.IFactory FactoryRef { get; }
    ICondition.IFactory ICondition.FactoryRef => FactoryRef;

    string ICondition.ValueName => AsNumber().ToString();

    bool SatisfiedBy(CurrentConditions currentConditions, Func<int, int, bool> compare) {
        var currentValue = FactoryRef.FromConditions(currentConditions);
        if (currentValue == null) { return false; }
        return compare(currentValue.AsNumber(), this.AsNumber());
    }

    public new interface IFactory : ICondition.IFactory {
        INumericCondition? FromConditions(CurrentConditions conditions);
        INumericCondition FromNumber(int num);

        public new INumericCondition? BestInitialValue() => Current();
        ICondition? ICondition.IFactory.BestInitialValue() => BestInitialValue();

        public new INumericCondition Default() => FromNumber(0);
        ICondition ICondition.IFactory.Default() => Default();

        public new INumericCondition? Current();
        ICondition? ICondition.IFactory.Current() => Current();
    }
}
