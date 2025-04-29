using System;
using MacroMate.Conditions;

namespace MacroMate.Serialization.V1;

public class OpExprXML {
    public enum OpName { IS, IS_NOT, LT, LTE, GT, GTE }

    public required ConditionXML Condition { get; set; }
    public required OpName Op { get; set; }

    public OpExpr ToReal() {
        var condition = Condition.ToReal();

        // All-condition operators
        switch (Op) {
            case OpName.IS: return new OpExpr.Is(condition);
            case OpName.IS_NOT: return new OpExpr.IsNot(condition);
        }

        // Numeric operators
        if (condition is INumericCondition numCondition) {
            switch (Op) {
                case OpName.LT:  return new OpExpr.Lt(numCondition);
                case OpName.LTE: return new OpExpr.Lte(numCondition);
                case OpName.GT:  return new OpExpr.Gt(numCondition);
                case OpName.GTE: return new OpExpr.Gte(numCondition);
            }
        }

        throw new Exception($"Invalid OpExpr: {Op}: {condition}");
    }

    public static OpExprXML From(OpExpr op) {
        var opName = op switch {
            OpExpr.Is  => OpName.IS,
            OpExpr.IsNot => OpName.IS_NOT,
            OpExpr.Lt  => OpName.LT,
            OpExpr.Lte => OpName.LTE,
            OpExpr.Gt  => OpName.GT,
            OpExpr.Gte => OpName.GTE,
            _ => throw new Exception($"Unexpected op expr {op}")
        };

        return new OpExprXML {
            Condition = ConditionXML.From(op.Condition),
            Op = opName
        };
    }
}
