using System.Globalization;

namespace ClassLibrary.Wp;

/// <summary>Builds, simplifies, substitutes, and renders symbolic wp conditions.</summary>
internal static class WpLogic
{
    private static readonly BooleanExpression True = new BooleanConstant(true);

    /// <summary>Normalizes a Boolean tree from its leaves toward its root.</summary>
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

    /// <summary>Requires nonzero divisors and nonnegative square-root arguments recursively.</summary>
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

    /// <summary>Accounts for short-circuit evaluation when finding a condition's domain.</summary>
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

    /// <summary>Replaces an assigned variable throughout a Boolean target condition.</summary>
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

    /// <summary>Performs the same substitution inside nested arithmetic expressions.</summary>
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

    /// <summary>Combines requirements while folding constants and known contradictions.</summary>
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

    /// <summary>Simplifies a nested condition under a comparison already known to hold.</summary>
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

    /// <summary>Combines alternatives while folding constants and duplicate terms.</summary>
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

    /// <summary>Negates a condition and reverses simple comparisons directly.</summary>
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

    /// <summary>Folds a comparison when possible and moves a constant offset to the right.</summary>
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

    /// <summary>Simplifies arithmetic recursively without evaluating symbolic variables.</summary>
    public static ArithmeticExpression Simplify(ArithmeticExpression expression) => expression switch
    {
        NumberExpression or VariableExpression => expression,
        NegativeExpression negative => Negate(Simplify(negative.Operand)),
        ArithmeticBinaryExpression binary => Arithmetic(
            Simplify(binary.Left), binary.Operator, Simplify(binary.Right)),
        FunctionExpression function => new FunctionExpression(function.Name, Simplify(function.Argument)),
        _ => throw new InvalidOperationException("Неизвестное арифметическое выражение")
    };

    /// <summary>Removes double negation or folds a safely negated constant.</summary>
    private static ArithmeticExpression Negate(ArithmeticExpression value)
    {
        if (value is NegativeExpression nested) return nested.Operand;
        if (value is NumberExpression number && TrySubtract(0, number.Value, out var result))
            return new NumberExpression(result);
        return new NegativeExpression(value);
    }

    /// <summary>Folds safe arithmetic, neutral elements, and adjacent constant offsets.</summary>
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

    /// <summary>Separates a trailing chain of additions and subtractions from its symbolic core.</summary>
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

    /// <summary>Rebuilds a symbolic expression with one normalized constant offset.</summary>
    private static ArithmeticExpression WithOffset(ArithmeticExpression core, decimal offset)
    {
        if (offset == 0) return core;
        return offset > 0
            ? new ArithmeticBinaryExpression(core, ArithmeticOperator.Add, new NumberExpression(offset))
            : TrySubtract(0, offset, out var magnitude)
                ? new ArithmeticBinaryExpression(core, ArithmeticOperator.Subtract, new NumberExpression(magnitude))
                : new ArithmeticBinaryExpression(core, ArithmeticOperator.Add, new NumberExpression(offset));
    }

    /// <summary>Folds exact decimal operations; division stays symbolic to avoid rounding.</summary>
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

    /// <summary>Performs checked decimal subtraction without leaking overflow exceptions.</summary>
    private static bool TrySubtract(decimal left, decimal right, out decimal result)
    {
        try { result = checked(left - right); return true; }
        catch (OverflowException) { result = 0; return false; }
    }

    /// <summary>Renders arithmetic with parentheses that preserve operator precedence.</summary>
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

    /// <summary>Renders a condition in the parser's symbolic input syntax.</summary>
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

    /// <summary>Renders a condition as a Russian-language explanation for the UI.</summary>
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
            ComparisonExpression comparison => DescribeComparison(comparison),
            NotExpression not => "не (" + Describe(not.Operand) + ")",
            BooleanBinaryExpression binary => Describe(binary.Left, precedence) +
                (binary.Operator == BooleanOperator.And ? " и " : " или ") +
                Describe(binary.Right, precedence),
            _ => throw new InvalidOperationException("Неизвестное логическое выражение")
        };
        return precedence < parentPrecedence ? "(" + text + ")" : text;
    }

    /// <summary>Groups conjunctions as requirements and disjunctions as alternative scenarios.</summary>
    public static string Explain(BooleanExpression expression)
    {
        if (expression is not BooleanBinaryExpression binary)
            return Describe(expression);

        var terms = new List<BooleanExpression>();
        CollectTerms(expression, binary.Operator, terms);
        if (binary.Operator == BooleanOperator.And)
            return "Одновременно должны выполняться условия:" + Environment.NewLine +
                   string.Join(Environment.NewLine, terms.Select(term => "• " + Describe(term)));

        var alternatives = new List<string>();
        for (var i = 0; i < terms.Count; i++)
        {
            var requirements = new List<BooleanExpression>();
            CollectTerms(terms[i], BooleanOperator.And, requirements);
            alternatives.Add((i + 1) + ". " +
                             string.Join("; ", requirements.Select(requirement => Describe(requirement))));
        }
        return "Достаточно одного из вариантов:" + Environment.NewLine +
               string.Join(Environment.NewLine, alternatives);
    }

    /// <summary>Flattens adjacent terms with the same operator without changing logical grouping.</summary>
    private static void CollectTerms(BooleanExpression expression, BooleanOperator operation,
                                     List<BooleanExpression> terms)
    {
        if (expression is BooleanBinaryExpression binary && binary.Operator == operation)
        {
            CollectTerms(binary.Left, operation, terms);
            CollectTerms(binary.Right, operation, terms);
        }
        else
        {
            terms.Add(expression);
        }
    }

    /// <summary>Explains state flags as car-sharing facts and other comparisons as numeric conditions.</summary>
    private static string DescribeComparison(ComparisonExpression comparison)
    {
        if (comparison.Left is VariableExpression variable &&
            comparison.Right is NumberExpression { Value: 0 or 1 } flag &&
            comparison.Operator == ComparisonOperator.Equal &&
            TryDescribeFlag(variable.Name, flag.Value == 1, out var fact))
            return fact;

        if (comparison.Left is ArithmeticBinaryExpression
            {
                Left: NumberExpression { Value: 2 },
                Operator: ArithmeticOperator.Multiply,
                Right: VariableExpression { Name: "a" }
            } && comparison.Operator == ComparisonOperator.NotEqual &&
            comparison.Right is NumberExpression { Value: 0 })
            return "коэффициент a модели движения должен быть ненулевым";

        var relation = comparison.Operator switch
        {
            ComparisonOperator.Equal => "равно",
            ComparisonOperator.NotEqual => "не равно",
            ComparisonOperator.Less => "меньше",
            ComparisonOperator.LessOrEqual => "не больше",
            ComparisonOperator.Greater => "больше",
            _ => "не меньше"
        };
        return DescribeArithmetic(comparison.Left) + " " + relation + " " +
               DescribeArithmetic(comparison.Right);
    }

    /// <summary>Maps the numeric 0/1 convention of preset flags to domain-specific statements.</summary>
    private static bool TryDescribeFlag(string name, bool enabled, out string fact)
    {
        fact = name switch
        {
            "carAvailable" => enabled ? "автомобиль доступен" : "автомобиль недоступен",
            "userLoggedIn" => enabled ? "пользователь авторизован" : "пользователь не авторизован",
            "activeRent" => enabled ? "у пользователя есть активная аренда" : "у пользователя нет активной аренды",
            "rentalStarted" => enabled ? "аренда успешно начата" : "аренда не начата",
            "returnCompleted" => enabled ? "поездка успешно завершена" : "поездка не завершена",
            "topUpCompleted" => enabled ? "баланс успешно пополнен" : "баланс не пополнен",
            _ => string.Empty
        };
        return fact.Length != 0;
    }

    /// <summary>Explains arithmetic in car-sharing terms while retaining unknown variable names.</summary>
    private static string DescribeArithmetic(ArithmeticExpression expression) => expression switch
    {
        NumberExpression => Format(expression),
        VariableExpression variable => variable.Name switch
        {
            "balance" => "баланс пользователя в рублях",
            "distance" => "расстояние поездки в километрах",
            "tariff" => "тариф за километр в рублях",
            "topUpAmount" => "сумма пополнения в рублях",
            "car1Mileage" => "пробег первого автомобиля",
            "car2Mileage" => "пробег второго автомобиля",
            "maxMileage" => "максимальный пробег двух автомобилей",
            "mileage" => "пробег автомобиля",
            "checkpointKm" => "показание одометра на контрольной точке",
            "carSpeed" => "рассчитанная скорость автомобиля",
            "D" => "дискриминант модели движения",
            "a" => "коэффициент a модели движения",
            "b" => "коэффициент b модели движения",
            "c" => "коэффициент c модели движения",
            _ => variable.Name
        },
        NegativeExpression negative => "отрицательное значение (" + DescribeArithmetic(negative.Operand) + ")",
        FunctionExpression { Name: "abs" } function => "модуль (" + DescribeArithmetic(function.Argument) + ")",
        FunctionExpression { Name: "sqrt" } function => "квадратный корень из (" + DescribeArithmetic(function.Argument) + ")",
        ArithmeticBinaryExpression
        {
            Left: ArithmeticBinaryExpression
            {
                Left: VariableExpression { Name: "b" },
                Operator: ArithmeticOperator.Multiply,
                Right: VariableExpression { Name: "b" }
            },
            Operator: ArithmeticOperator.Subtract,
            Right: ArithmeticBinaryExpression
            {
                Left: ArithmeticBinaryExpression
                {
                    Left: NumberExpression { Value: 4 },
                    Operator: ArithmeticOperator.Multiply,
                    Right: VariableExpression { Name: "a" }
                },
                Operator: ArithmeticOperator.Multiply,
                Right: VariableExpression { Name: "c" }
            }
        } => "дискриминант модели движения",
        ArithmeticBinaryExpression
        {
            Left: NumberExpression { Value: 2 },
            Operator: ArithmeticOperator.Multiply,
            Right: VariableExpression { Name: "a" }
        } => "удвоенный коэффициент a модели движения",
        ArithmeticBinaryExpression
        {
            Left: VariableExpression { Name: "distance" },
            Operator: ArithmeticOperator.Multiply,
            Right: VariableExpression { Name: "tariff" }
        } => "стоимость поездки",
        ArithmeticBinaryExpression
        {
            Left: VariableExpression { Name: "balance" },
            Operator: ArithmeticOperator.Subtract,
            Right: ArithmeticBinaryExpression
            {
                Left: VariableExpression { Name: "distance" },
                Operator: ArithmeticOperator.Multiply,
                Right: VariableExpression { Name: "tariff" }
            }
        } => "остаток баланса после оплаты поездки",
        ArithmeticBinaryExpression
        {
            Left: VariableExpression { Name: "balance" },
            Operator: ArithmeticOperator.Add,
            Right: VariableExpression { Name: "topUpAmount" }
        } => "баланс после пополнения",
        // Keep large symbolic formulas compact; the exact expression remains visible in the wp field.
        ArithmeticBinaryExpression binary => "значение выражения «" + Format(binary) + "»",
        _ => throw new InvalidOperationException("Неизвестное арифметическое выражение")
    };
}
