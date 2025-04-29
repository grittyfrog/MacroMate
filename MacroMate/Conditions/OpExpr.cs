using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MacroMate.Conditions;

public interface OpExpr {
    public string Text { get; }
    public string? LongText { get => null; }
    public bool SatisfiedBy(CurrentConditions currentConditions);
    public ICondition Condition { get; }

    bool TrySetCondition(ICondition condition, [MaybeNullWhen(false)] out OpExpr updated) {
        updated = null;
        if (condition is IValueCondition valueCondition) {
            switch (this) {
                case OpExpr.Is isOp: updated = isOp with { Condition = valueCondition }; break;
                case OpExpr.IsNot isNotOp: updated = isNotOp with { Condition = valueCondition }; break;
            }
        } else if (condition is INumericCondition numCondition) {
            switch (this) {
                case OpExpr.Is isOp: updated = isOp with { Condition = numCondition }; break;
                case OpExpr.Lt numOp: updated = numOp with { Num = numCondition }; break;
                case OpExpr.Lte numOp: updated = numOp with { Num = numCondition }; break;
                case OpExpr.Gte numOp: updated = numOp with { Num = numCondition }; break;
                case OpExpr.Gt numOp: updated = numOp with { Num = numCondition }; break;
            }
        }

        return updated != null;
    }

    /// Wraps condition in all valid operators
    public static IEnumerable<OpExpr> WrapAll(ICondition condition) {
        yield return new OpExpr.Is(condition);
        yield return new OpExpr.IsNot(condition);

        if (condition is INumericCondition numCondition) {
            yield return new OpExpr.Lt(numCondition);
            yield return new OpExpr.Lte(numCondition);
            yield return new OpExpr.Gt(numCondition);
            yield return new OpExpr.Gte(numCondition);
        }
    }

    /// True if the current conditions are the same as condition
    public record class Is(ICondition Condition) : OpExpr {
        public string Text => "is";
        public bool SatisfiedBy(CurrentConditions currentConditions) {
            if (Condition is IValueCondition valueCondition) {
                return valueCondition.SatisfiedBy(currentConditions);
            } else if (Condition is INumericCondition numCondition) {
                return numCondition.SatisfiedBy(currentConditions, (cur, val) => cur == val);
            }

            return false;
        }
    };

    /// True if the current conditions are not the same as condition
    public record class IsNot(ICondition Condition) : OpExpr {
        public string Text => "is not";
        public bool SatisfiedBy(CurrentConditions currentConditions) {
            if (Condition is IValueCondition valueCondition) {
                return !valueCondition.SatisfiedBy(currentConditions);
            } else if (Condition is INumericCondition numCondition) {
                return numCondition.SatisfiedBy(currentConditions, (cur, val) => cur != val);
            }

            return false;
        }
    }

    /// True if the current conditions are less than to num
    public record class Lt(INumericCondition Num) : OpExpr {
        public string Text => "<";
        public string LongText => "Less Than";
        public ICondition Condition => Num;
        public bool SatisfiedBy(CurrentConditions currentConditions) =>
            Num.SatisfiedBy(currentConditions, (cur, val) => cur < val);
    };

    /// True if the current conditions are less than or equal to num
    public record class Lte(INumericCondition Num) : OpExpr {
        public string Text => "<=";
        public string LongText => "Less Than or Equal To";
        public ICondition Condition => Num;
        public bool SatisfiedBy(CurrentConditions currentConditions) =>
            Num.SatisfiedBy(currentConditions, (cur, val) => cur <= val);
    }

    /// True if the current conditions are greater than to num
    public record class Gt(INumericCondition Num) : OpExpr {
        public string Text => ">";
        public string LongText => "Greater Than";
        public ICondition Condition => Num;
        public bool SatisfiedBy(CurrentConditions currentConditions) =>
            Num.SatisfiedBy(currentConditions, (cur, val) => cur > val);
    }

    /// True if the current conditions are grater than or equal to num
    public record class Gte(INumericCondition Num) : OpExpr {
        public string Text => ">=";
        public string LongText => "Greater Than or Equal To";
        public ICondition Condition => Num;
        public bool SatisfiedBy(CurrentConditions currentConditions) =>
            Num.SatisfiedBy(currentConditions, (cur, val) => cur >= val);
    }
}
