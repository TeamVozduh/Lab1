using System.Globalization;

namespace ClassLibrary.Wp;

internal static class WpLogic
{
    private static readonly BooleanExpression True = new BooleanConstant(true);

    public static BooleanExpression Simplify(BooleanExpression expression) => expression switch
    {
        BooleanConstant => expression,
        ComparisonExpression comparison => Compare(comparison.Left, comparison.Operator, comparison.Right),
        NotExpression not => Not(Simplify(not.Operand)),
        BooleanBinaryExpression binary when binary.Operator == BooleanOperator.And =>
            And(Simplify(binary.Left), Simplify(binary.Right)),
        BooleanBinaryExpression binary => Or(Simplify(binary.Left), Simplify(binary.Right)),
        _ => throw new InvalidOperationException("Неизвестное логическое выражение")
    };

    public static BooleanExpression Defined(ArithmeticExpression expression) => expression switch
    {
        NumberExpression or VariableExpression => True,
        NegativeExpression negative => Defined(negative.Operand),
        ArithmeticBinaryExpression binary when binary.Operator == ArithmeticOperator.Divide =>
            And(And(Defined(binary.Left), Defined(binary.Right)),
                Compare(binary.Right, ComparisonOperator.NotEqual, new NumberExpression(0))),
        ArithmeticBinaryExpression binary => And(Defined(binary.Left), Defined(binary.Right)),
        FunctionExpression function when function.Name == "sqrt" =>
            And(Defined(function.Argument),
                Compare(function.Argument, ComparisonOperator.GreaterOrEqual, new NumberExpression(0))),
        FunctionExpression function => Defined(function.Argument),
        _ => throw new InvalidOperationException("Неизвестное арифметическое выражение")
    };

    public static BooleanExpression Defined(BooleanExpression expression) => expression switch
    {
        BooleanConstant => True,
        ComparisonExpression comparison =>
            And(Defined(comparison.Left), Defined(comparison.Right)),
        NotExpression not => Defined(not.Operand),
        BooleanBinaryExpression binary when binary.Operator == BooleanOperator.And =>
            And(Defined(binary.Left), Or(Not(binary.Left), Defined(binary.Right))),
        BooleanBinaryExpression binary =>
            And(Defined(binary.Left), Or(binary.Left, Defined(binary.Right))),
        _ => throw new InvalidOperationException("Неизвестное логическое выражение")
    };

    public static BooleanExpression Substitute(BooleanExpression expression, string variable,
                                               ArithmeticExpression replacement) => expression switch
    {
        BooleanConstant => expression,
        ComparisonExpression comparison => Compare(
            Substitute(comparison.Left, variable, replacement), comparison.Operator,
            Substitute(comparison.Right, variable, replacement)),
        NotExpression not => Not(Substitute(not.Operand, variable, replacement)),
        BooleanBinaryExpression binary when binary.Operator == BooleanOperator.And => And(
            Substitute(binary.Left, variable, replacement),
            Substitute(binary.Right, variable, replacement)),
        BooleanBinaryExpression binary => Or(
            Substitute(binary.Left, variable, replacement),
            Substitute(binary.Right, variable, replacement)),
        _ => throw new InvalidOperationException("Неизвестное логическое выражение")
    };

    private static ArithmeticExpression Substitute(ArithmeticExpression expression, string variable,
                                                   ArithmeticExpression replacement) => expression switch
    {
        VariableExpression name when name.Name == variable => replacement,
        NumberExpression or VariableExpression => expression,
        NegativeExpression negative => Negate(Substitute(negative.Operand, variable, replacement)),
        ArithmeticBinaryExpression binary => Arithmetic(
            Substitute(binary.Left, variable, replacement), binary.Operator,
            Substitute(binary.Right, variable, replacement)),
        FunctionExpression function => new FunctionExpression(
            function.Name, Substitute(function.Argument, variable, replacement)),
        _ => throw new InvalidOperationException("Неизвестное арифметическое выражение")
    };

    public static BooleanExpression And(BooleanExpression left, BooleanExpression right)
    {
        if (left is BooleanConstant { Value: false } || right is BooleanConstant { Value: false })
            return new BooleanConstant(false);
        if (left is BooleanConstant { Value: true }) return right;
        if (right is BooleanConstant { Value: true }) return left;
        if (left == right) return left;
        if (left == Not(right) || right == Not(left)) return new BooleanConstant(false);
        if (left is ComparisonExpression)
            right = SimplifyAssuming(right, left);
        else if (right is ComparisonExpression)
            left = SimplifyAssuming(left, right);
        if (left is BooleanConstant { Value: true }) return right;
        if (right is BooleanConstant { Value: true }) return left;
        if (left is BooleanConstant { Value: false } || right is BooleanConstant { Value: false })
            return new BooleanConstant(false);
        return new BooleanBinaryExpression(left, BooleanOperator.And, right);
    }

    private static BooleanExpression SimplifyAssuming(BooleanExpression expression, BooleanExpression fact)
    {
        if (expression == fact) return new BooleanConstant(true);
        if (expression == Not(fact)) return new BooleanConstant(false);
        return expression switch
        {
            BooleanBinaryExpression binary when binary.Operator == BooleanOperator.And =>
                And(SimplifyAssuming(binary.Left, fact), SimplifyAssuming(binary.Right, fact)),
            BooleanBinaryExpression binary =>
                Or(SimplifyAssuming(binary.Left, fact), SimplifyAssuming(binary.Right, fact)),
            NotExpression not => Not(SimplifyAssuming(not.Operand, fact)),
            _ => expression
        };
    }

    public static BooleanExpression Or(BooleanExpression left, BooleanExpression right)
    {
        if (left is BooleanConstant { Value: true } || right is BooleanConstant { Value: true })
            return new BooleanConstant(true);
        if (left is BooleanConstant { Value: false }) return right;
        if (right is BooleanConstant { Value: false }) return left;
        if (left == right) return left;
        if (left == Not(right) || right == Not(left)) return new BooleanConstant(true);
        return new BooleanBinaryExpression(left, BooleanOperator.Or, right);
    }

    public static BooleanExpression Not(BooleanExpression value) => value switch
    {
        BooleanConstant constant => new BooleanConstant(!constant.Value),
        NotExpression nested => nested.Operand,
        ComparisonExpression comparison => Compare(comparison.Left,
            comparison.Operator switch
            {
                ComparisonOperator.Equal => ComparisonOperator.NotEqual,
                ComparisonOperator.NotEqual => ComparisonOperator.Equal,
                ComparisonOperator.Less => ComparisonOperator.GreaterOrEqual,
                ComparisonOperator.LessOrEqual => ComparisonOperator.Greater,
                ComparisonOperator.Greater => ComparisonOperator.LessOrEqual,
                _ => ComparisonOperator.Less
            }, comparison.Right),
        _ => new NotExpression(value)
    };

    public static BooleanExpression Compare(ArithmeticExpression left, ComparisonOperator operation,
                                            ArithmeticExpression right)
    {
        left = Simplify(left);
        right = Simplify(right);

        if (left == right)
            return new BooleanConstant(operation is ComparisonOperator.Equal or
                ComparisonOperator.LessOrEqual or ComparisonOperator.GreaterOrEqual);

        if (left is NumberExpression a && right is NumberExpression b)
            return new BooleanConstant(operation switch
            {
                ComparisonOperator.Equal => a.Value == b.Value,
                ComparisonOperator.NotEqual => a.Value != b.Value,
                ComparisonOperator.Less => a.Value < b.Value,
                ComparisonOperator.LessOrEqual => a.Value <= b.Value,
                ComparisonOperator.Greater => a.Value > b.Value,
                _ => a.Value >= b.Value
            });

        // Move a constant offset to the right. This turns x + 10 > 15 into x > 5.
        if (right is NumberExpression limit && TryOffset(left, out var core, out var offset) &&
            TrySubtract(limit.Value, offset, out var shifted))
            return new ComparisonExpression(core, operation, new NumberExpression(shifted));

        return new ComparisonExpression(left, operation, right);
    }

    public static ArithmeticExpression Simplify(ArithmeticExpression expression) => expression switch
    {
        NumberExpression or VariableExpression => expression,
        NegativeExpression negative => Negate(Simplify(negative.Operand)),
        ArithmeticBinaryExpression binary => Arithmetic(
            Simplify(binary.Left), binary.Operator, Simplify(binary.Right)),
        FunctionExpression function => new FunctionExpression(function.Name, Simplify(function.Argument)),
        _ => throw new InvalidOperationException("Неизвестное арифметическое выражение")
    };

    private static ArithmeticExpression Negate(ArithmeticExpression value)
    {
        if (value is NegativeExpression nested) return nested.Operand;
        if (value is NumberExpression number && TrySubtract(0, number.Value, out var result))
            return new NumberExpression(result);
        return new NegativeExpression(value);
    }

    private static ArithmeticExpression Arithmetic(ArithmeticExpression left, ArithmeticOperator operation,
                                                   ArithmeticExpression right)
    {
        if (left is NumberExpression a && right is NumberExpression b &&
            TryCalculate(a.Value, operation, b.Value, out var value))
            return new NumberExpression(value);

        if (right is NumberExpression { Value: 0 } && operation is ArithmeticOperator.Add or ArithmeticOperator.Subtract)
            return left;
        if (left is NumberExpression { Value: 0 } && operation == ArithmeticOperator.Add)
            return right;
        if (right is NumberExpression { Value: 1 } && operation is ArithmeticOperator.Multiply or ArithmeticOperator.Divide)
            return left;
        if (left is NumberExpression { Value: 1 } && operation == ArithmeticOperator.Multiply)
            return right;
        if ((left is NumberExpression { Value: 0 } || right is NumberExpression { Value: 0 }) &&
            operation == ArithmeticOperator.Multiply)
            return new NumberExpression(0);

        if (right is NumberExpression constant &&
            operation is ArithmeticOperator.Add or ArithmeticOperator.Subtract &&
            TryOffset(left, out var core, out var offset) &&
            TryCalculate(offset, operation, constant.Value, out var combined))
            return WithOffset(core, combined);

        return new ArithmeticBinaryExpression(left, operation, right);
    }

    private static bool TryOffset(ArithmeticExpression expression, out ArithmeticExpression core,
                                  out decimal offset)
    {
        if (expression is ArithmeticBinaryExpression binary &&
            binary.Operator is ArithmeticOperator.Add or ArithmeticOperator.Subtract &&
            binary.Right is NumberExpression number)
        {
            if (TryOffset(binary.Left, out core, out var previous) &&
                TryCalculate(previous, binary.Operator, number.Value, out offset))
                return true;

            core = binary.Left;
            if (binary.Operator == ArithmeticOperator.Add)
            {
                offset = number.Value;
                return true;
            }
            return TrySubtract(0, number.Value, out offset);
        }

        core = expression;
        offset = 0;
        return false;
    }

    private static ArithmeticExpression WithOffset(ArithmeticExpression core, decimal offset)
    {
        if (offset == 0) return core;
        return offset > 0
            ? new ArithmeticBinaryExpression(core, ArithmeticOperator.Add, new NumberExpression(offset))
            : TrySubtract(0, offset, out var magnitude)
                ? new ArithmeticBinaryExpression(core, ArithmeticOperator.Subtract, new NumberExpression(magnitude))
                : new ArithmeticBinaryExpression(core, ArithmeticOperator.Add, new NumberExpression(offset));
    }

    private static bool TryCalculate(decimal left, ArithmeticOperator operation, decimal right, out decimal result)
    {
        result = 0;
        // Decimal division can round (1 / 3, for example). Keep division symbolic.
        if (operation == ArithmeticOperator.Divide) return false;
        try
        {
            result = operation switch
            {
                ArithmeticOperator.Add => checked(left + right),
                ArithmeticOperator.Subtract => checked(left - right),
                ArithmeticOperator.Multiply => checked(left * right),
                _ => throw new InvalidOperationException("Неизвестная операция")
            };
            return true;
        }
        catch (OverflowException) { return false; }
    }

    private static bool TrySubtract(decimal left, decimal right, out decimal result)
    {
        try { result = checked(left - right); return true; }
        catch (OverflowException) { result = 0; return false; }
    }

    public static string Format(ArithmeticExpression expression, int parentPrecedence = 0)
    {
        var precedence = expression switch
        {
            ArithmeticBinaryExpression { Operator: ArithmeticOperator.Add or ArithmeticOperator.Subtract } => 1,
            ArithmeticBinaryExpression => 2,
            NegativeExpression => 3,
            _ => 4
        };

        string text = expression switch
        {
            NumberExpression number => number.Value.ToString(CultureInfo.InvariantCulture),
            VariableExpression variable => variable.Name,
            NegativeExpression negative => "-" + Format(negative.Operand, precedence),
            FunctionExpression function => function.Name + "(" + Format(function.Argument) + ")",
            ArithmeticBinaryExpression binary => Format(binary.Left, precedence) + " " +
                (binary.Operator switch
                {
                    ArithmeticOperator.Add => "+",
                    ArithmeticOperator.Subtract => "-",
                    ArithmeticOperator.Multiply => "*",
                    _ => "/"
                }) + " " + Format(binary.Right, precedence +
                    (binary.Operator is ArithmeticOperator.Subtract or ArithmeticOperator.Divide ? 1 : 0)),
            _ => throw new InvalidOperationException("Неизвестное арифметическое выражение")
        };
        return precedence < parentPrecedence ? "(" + text + ")" : text;
    }

    public static string Format(BooleanExpression expression, int parentPrecedence = 0)
    {
        var precedence = expression switch
        {
            BooleanBinaryExpression { Operator: BooleanOperator.Or } => 1,
            BooleanBinaryExpression => 2,
            NotExpression => 3,
            _ => 4
        };

        string text = expression switch
        {
            BooleanConstant constant => constant.Value ? "true" : "false",
            ComparisonExpression comparison => Format(comparison.Left) + " " +
                (comparison.Operator switch
                {
                    ComparisonOperator.Equal => "==",
                    ComparisonOperator.NotEqual => "!=",
                    ComparisonOperator.Less => "<",
                    ComparisonOperator.LessOrEqual => "<=",
                    ComparisonOperator.Greater => ">",
                    _ => ">="
                }) + " " + Format(comparison.Right),
            NotExpression not => "!(" + Format(not.Operand) + ")",
            BooleanBinaryExpression binary => Format(binary.Left, precedence) +
                (binary.Operator == BooleanOperator.And ? " && " : " || ") +
                Format(binary.Right, precedence),
            _ => throw new InvalidOperationException("Неизвестное логическое выражение")
        };
        return precedence < parentPrecedence ? "(" + text + ")" : text;
    }

    public static string Describe(BooleanExpression expression, int parentPrecedence = 0)
    {
        var precedence = expression switch
        {
            BooleanBinaryExpression { Operator: BooleanOperator.Or } => 1,
            BooleanBinaryExpression => 2,
            NotExpression => 3,
            _ => 4
        };
        string text = expression switch
        {
            BooleanConstant { Value: true } => "условие выполняется всегда",
            BooleanConstant => "условие невыполнимо",
            ComparisonExpression comparison => Format(comparison.Left) + " " +
                (comparison.Operator switch
                {
                    ComparisonOperator.Equal => "равно",
                    ComparisonOperator.NotEqual => "не равно",
                    ComparisonOperator.Less => "меньше",
                    ComparisonOperator.LessOrEqual => "не больше",
                    ComparisonOperator.Greater => "больше",
                    _ => "не меньше"
                }) + " " + Format(comparison.Right),
            NotExpression not => "не (" + Describe(not.Operand) + ")",
            BooleanBinaryExpression binary => Describe(binary.Left, precedence) +
                (binary.Operator == BooleanOperator.And ? " и " : " или ") +
                Describe(binary.Right, precedence),
            _ => throw new InvalidOperationException("Неизвестное логическое выражение")
        };
        return precedence < parentPrecedence ? "(" + text + ")" : text;
    }
}
