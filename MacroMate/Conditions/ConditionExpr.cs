using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MacroMate.Conditions;

/// A condition expression, currently constaint to an Or of Ands.
public interface ConditionExpr {
    public record class Or(ImmutableList<Conditions.ConditionExpr.And> options) : Conditions.ConditionExpr {
        public static Conditions.ConditionExpr.Or Empty => new Conditions.ConditionExpr.Or(ImmutableList<And>.Empty);

        public bool SatisfiedBy(CurrentConditions currentConditions) {
            // If we have no conditions then we are never satisfied
            if (options.IsEmpty) { return false; }

            return options.Any(option => option.SatisfiedBy(currentConditions));
        }

        public Conditions.ConditionExpr.Or AddAnd() {
            return AddAnd(Conditions.ConditionExpr.And.Empty);
        }

        public Conditions.ConditionExpr.Or AddAnd(ConditionExpr.And and) {
            return this with { options = options.Add(and) };
        }

        public Conditions.ConditionExpr.Or DeleteAnd(int andIndex)  {
            return this with { options = options.RemoveAt(andIndex) };
        }

        public Conditions.ConditionExpr.Or UpdateAnd(
            int andIndex,
            Func<Conditions.ConditionExpr.And, Conditions.ConditionExpr.And> update
        )  {
            var andCondition = options[andIndex];
            var updatedAndCondition = update(andCondition);
            return this with { options = options.SetItem(andIndex, updatedAndCondition) };
        }

        public static Conditions.ConditionExpr.Or Single(IValueCondition condition) =>
            new ConditionExpr.Or(
                ImmutableList.Create(new ConditionExpr.And(ImmutableList.Create(condition.WrapInDefaultOp())))
            );
    }

    public record class And(ImmutableList<OpExpr> opExprs) : Conditions.ConditionExpr {
        public static Conditions.ConditionExpr.And Empty => new Conditions.ConditionExpr.And(ImmutableList<OpExpr>.Empty);
        public static Conditions.ConditionExpr.And Single(OpExpr condition) =>
            new Conditions.ConditionExpr.And((new[] { condition }).ToImmutableList());

        public bool SatisfiedBy(CurrentConditions currentConditions) {
            // If we have no conditions then we are never satisfied
            if (opExprs.IsEmpty) { return false; }

            return opExprs.All(condition => condition.SatisfiedBy(currentConditions));
        }

        public Conditions.ConditionExpr.And AddCondition(OpExpr conditionOp) =>
            this with { opExprs = opExprs.Add(conditionOp) };
        public Conditions.ConditionExpr.And AddConditions(IEnumerable<OpExpr> condition) =>
            condition.Aggregate(this, (and, condition) => and.AddCondition(condition));
        public Conditions.ConditionExpr.And DeleteCondition(int conditionIndex) =>
            this with { opExprs = opExprs.RemoveAt(conditionIndex) };

        public Conditions.ConditionExpr.And SetCondition(int conditionIndex, ICondition condition) {
            var currentCondition = opExprs[conditionIndex];
            if (currentCondition.TrySetCondition(condition, out var updatedCondition)) {
                return this with { opExprs = opExprs.SetItem(conditionIndex, updatedCondition) };
            } else {
                Env.PluginLog.Error($"Could not set condition {condition} on index {conditionIndex}");
                return this;
            }
        }

        public Conditions.ConditionExpr.And SetOperator(int conditionIndex, OpExpr op) {
            return this with { opExprs = opExprs.SetItem(conditionIndex, op) };
        }
    }

    public bool SatisfiedBy(CurrentConditions currentConditions);
}
