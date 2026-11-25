using ClassLibrary.Wp;

namespace ClassLibrary.Loops;

/// <summary>
/// Исполнитель деревьев ЛР2 для конечного состояния цикла. Символьный wp остаётся
/// в WpEngine; этот класс только проверяет полученные условия на конкретных данных.
/// </summary>
internal sealed class LoopExpressions(IReadOnlyList<int> data, int threshold)
{
    /// <summary>Проверяет все имена, в том числе в невычисляемых ветвях. t0 зарезервировано для wp завершения.</summary>
    public static void Validate(BooleanExpression expression)
    {
        switch (expression)
        {
            case ComparisonExpression value: Validate(value.Left); Validate(value.Right); break;
            case NotExpression value: Validate(value.Operand); break;
            case BooleanBinaryExpression value: Validate(value.Left); Validate(value.Right); break;
        }
    }

    public static void Validate(ArithmeticExpression expression)
    {
        switch (expression)
        {
            case VariableExpression value when value.Name is not ("j" or "n" or "res" or "T"):
                throw new ArgumentException($"Переменная «{value.Name}» недоступна. Используйте j, n, res, T.");
            case NegativeExpression value: Validate(value.Operand); break;
            case ArithmeticBinaryExpression value: Validate(value.Left); Validate(value.Right); break;
            case FunctionExpression value: Validate(value.Argument); break;
        }
    }

    /// <summary>Вычисляет условие с той же короткой схемой && и ||, что и движок wp.</summary>
    public bool Test(BooleanExpression expression, IReadOnlyDictionary<string, decimal> state) => expression switch
    {
        BooleanConstant value => value.Value,
        NotExpression value => !Test(value.Operand, state),
        BooleanBinaryExpression value when value.Operator == BooleanOperator.And =>
            Test(value.Left, state) && Test(value.Right, state),
        BooleanBinaryExpression value => Test(value.Left, state) || Test(value.Right, state),
        ComparisonExpression value => Compare(Value(value.Left, state), value.Operator, Value(value.Right, state)),
        _ => throw new InvalidOperationException("Неизвестное условие")
    };

    /// <summary>Вычисляет арифметику; недопустимые индексы, имена и области определения дают ошибку.</summary>
    public decimal Value(ArithmeticExpression expression, IReadOnlyDictionary<string, decimal> state) => expression switch
    {
        NumberExpression value => value.Value,
        VariableExpression value => state.TryGetValue(value.Name, out var number) ? number :
            throw new ArgumentException($"Неизвестная переменная «{value.Name}». Доступны j, n, res, T, t0."),
        NegativeExpression value => checked(-Value(value.Operand, state)),
        ArithmeticBinaryExpression value => Calculate(Value(value.Left, state), value.Operator, Value(value.Right, state)),
        FunctionExpression value => Function(value.Name, Value(value.Argument, state)),
        _ => throw new InvalidOperationException("Неизвестное выражение")
    };

    /// <summary>Выполняет тело S из того же AST, по которому рассчитывается wp.</summary>
    public void Execute(IReadOnlyList<Statement> statements, Dictionary<string, decimal> state)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case AssignmentStatement assignment:
                    state[assignment.Variable] = Value(assignment.Value, state);
                    break;
                case IfStatement branch:
                    Execute(Test(branch.Condition, state) ? branch.ThenBranch : branch.ElseBranch, state);
                    break;
                default: throw new InvalidOperationException("Неизвестный оператор");
            }
        }
    }

    /// <summary>Проверяет целочисленную область индексов и затем вычисляет функцию префикса.</summary>
    private decimal Function(string name, decimal argument)
    {
        if (name == "abs") return Math.Abs(argument);
        if (name == "sqrt")
        {
            if (argument < 0) throw new ArgumentException("sqrt: аргумент должен быть неотрицательным.");
            return (decimal)Math.Sqrt((double)argument);
        }
        if (argument != decimal.Truncate(argument) || argument < 0 || argument > data.Count ||
            (name == "item" && argument == data.Count) || (name == "maximum" && argument == 0))
            throw new ArgumentException($"{name}({argument}): индекс или длина префикса вне области определения.");
        var length = (int)argument;
        return name switch
        {
            "item" => data[length],
            "sum" => data.Take(length).Sum(x => (decimal)x),
            "count" => data.Take(length).Count(x => x > threshold),
            "maximum" => data.Take(length).Max(),
            _ => throw new ArgumentException($"Неизвестная функция «{name}».")
        };
    }

    private static decimal Calculate(decimal a, ArithmeticOperator operation, decimal b) => operation switch
    {
        ArithmeticOperator.Add => checked(a + b),
        ArithmeticOperator.Subtract => checked(a - b),
        ArithmeticOperator.Multiply => checked(a * b),
        ArithmeticOperator.Divide => a / b,
        _ => throw new InvalidOperationException("Неизвестная операция")
    };

    private static bool Compare(decimal a, ComparisonOperator operation, decimal b) => operation switch
    {
        ComparisonOperator.Equal => a == b,
        ComparisonOperator.NotEqual => a != b,
        ComparisonOperator.Less => a < b,
        ComparisonOperator.LessOrEqual => a <= b,
        ComparisonOperator.Greater => a > b,
        ComparisonOperator.GreaterOrEqual => a >= b,
        _ => throw new InvalidOperationException("Неизвестное сравнение")
    };
}
