using System;
using System.Collections.Immutable;
using System.Linq;

namespace MacroMate.Conditions;

/// A condition expression, currently constaint to an Or of Ands.
public interface ConditionExpr {
    public record class Or(ImmutableList<ConditionExpr.And> options) : ConditionExpr {
        public static ConditionExpr.Or Empty => new ConditionExpr.Or(ImmutableList<ConditionExpr.And>.Empty);

        public bool SatisfiedBy(CurrentConditions currentConditions) {
            // If we have no conditions then we are never satisfied
            if (options.IsEmpty) { return false; }

            return options.Any(option => option.SatisfiedBy(currentConditions));
        }

        public ConditionExpr.Or AddAnd() {
            return this with { options = options.Add(ConditionExpr.And.Empty) };
        }

        public ConditionExpr.Or DeleteAnd(int andIndex)  {
            return this with { options = options.RemoveAt(andIndex) };
        }

        public ConditionExpr.Or UpdateAnd(
            int andIndex,
            Func<ConditionExpr.And, ConditionExpr.And> update
        )  {
            var andCondition = options[andIndex];
            var updatedAndCondition = update(andCondition);
            return this with { options = options.SetItem(andIndex, updatedAndCondition) };
        }

        public static Or Default() => new ConditionExpr.Or(
            Env.ConditionManager.CurrentConditions().Enumerate().Select(condition =>
                ConditionExpr.And.Single(condition)
            ).ToImmutableList()
        );

    }

    public record class And(ImmutableList<ICondition> conditions) : ConditionExpr {
        public static ConditionExpr.And Empty => new ConditionExpr.And(ImmutableList<ICondition>.Empty);
        public static ConditionExpr.And Single(ICondition condition) =>
            new ConditionExpr.And(new[] { condition }.ToImmutableList());

        public bool SatisfiedBy(CurrentConditions currentConditions) {
            // If we have no conditions then we are never satisfied
            if (conditions.IsEmpty) { return false; }

            return conditions.All(condition => condition.SatisfiedBy(currentConditions));
        }

        public ConditionExpr.And AddCondition(ICondition condition) =>
            this with { conditions = conditions.Add(condition) };
        public ConditionExpr.And DeleteCondition(int conditionIndex) =>
            this with { conditions = conditions.RemoveAt(conditionIndex) };
        public ConditionExpr.And SetCondition(int conditionIndex, ICondition condition) =>
            this with { conditions = conditions.SetItem(conditionIndex, condition) };
    }

    public bool SatisfiedBy(CurrentConditions currentConditions);
}
