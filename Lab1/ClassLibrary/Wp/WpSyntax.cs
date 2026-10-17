using System.Globalization;

namespace ClassLibrary.Wp;

/// <summary>Reports invalid wp input together with its zero-based source offset.</summary>
public sealed class WpParseException : FormatException
{
    /// <summary>Gets the source offset of the offending token or character.</summary>
    public int Position { get; }

    /// <summary>Creates a parse error whose message uses a one-based position.</summary>
    public WpParseException(string message, int position)
        : base($"{message} (позиция {position + 1}).")
    {
        Position = position;
    }
}

/// <summary>Base node for arithmetic expressions in assignments and comparisons.</summary>
internal abstract record ArithmeticExpression;
/// <summary>A decimal literal parsed independently of the machine's locale.</summary>
internal sealed record NumberExpression(decimal Value) : ArithmeticExpression;
/// <summary>A symbolic numeric variable.</summary>
internal sealed record VariableExpression(string Name) : ArithmeticExpression;
/// <summary>A unary minus applied to an arithmetic expression.</summary>
internal sealed record NegativeExpression(ArithmeticExpression Operand) : ArithmeticExpression;
/// <summary>A binary arithmetic operation with explicit operands.</summary>
internal sealed record ArithmeticBinaryExpression(
    ArithmeticExpression Left, ArithmeticOperator Operator, ArithmeticExpression Right) : ArithmeticExpression;
/// <summary>A supported unary function call, currently abs or sqrt.</summary>
internal sealed record FunctionExpression(string Name, ArithmeticExpression Argument) : ArithmeticExpression;

/// <summary>Operators accepted in arithmetic expressions.</summary>
internal enum ArithmeticOperator { Add, Subtract, Multiply, Divide }

/// <summary>Base node for Boolean conditions and postconditions.</summary>
internal abstract record BooleanExpression;
/// <summary>A literal true or false condition.</summary>
internal sealed record BooleanConstant(bool Value) : BooleanExpression;
/// <summary>A comparison between two arithmetic expressions.</summary>
internal sealed record ComparisonExpression(
    ArithmeticExpression Left, ComparisonOperator Operator, ArithmeticExpression Right) : BooleanExpression;
/// <summary>A logical negation of a condition.</summary>
internal sealed record NotExpression(BooleanExpression Operand) : BooleanExpression;
/// <summary>A conjunction or disjunction of two conditions.</summary>
internal sealed record BooleanBinaryExpression(
    BooleanExpression Left, BooleanOperator Operator, BooleanExpression Right) : BooleanExpression;

/// <summary>Comparison operators accepted in Boolean conditions.</summary>
internal enum ComparisonOperator { Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual }
/// <summary>Short-circuit Boolean operators accepted in conditions.</summary>
internal enum BooleanOperator { And, Or }

/// <summary>Base node for a statement in the loop-free input language.</summary>
internal abstract record Statement;
/// <summary>An assignment of an arithmetic expression to a variable.</summary>
internal sealed record AssignmentStatement(string Variable, ArithmeticExpression Value) : Statement;
/// <summary>A conditional with explicit then and else branches.</summary>
internal sealed record IfStatement(
    BooleanExpression Condition, IReadOnlyList<Statement> ThenBranch, IReadOnlyList<Statement> ElseBranch) : Statement;

/// <summary>Token categories recognized by the wp lexer.</summary>
internal enum TokenKind
{
    End, Identifier, Number, If, Else, Assign, Semicolon, LeftBrace, RightBrace,
    LeftParen, RightParen, Plus, Minus, Star, Slash, Greater, GreaterOrEqual,
    Less, LessOrEqual, Equal, NotEqual, Not, And, Or
}

/// <summary>A token with its spelling and zero-based source offset.</summary>
internal readonly record struct Token(TokenKind Kind, string Text, int Position);

/// <summary>Parses a loop-free program or a Boolean Post into syntax trees.</summary>
internal sealed class WpParser
{
    private readonly List<Token> tokens;
    private int index;

    /// <summary>Tokenizes the source for either parser entry point.</summary>
    public WpParser(string source)
    {
        tokens = Tokenize(source).ToList();
    }

    /// <summary>Parses a nonempty statement sequence and rejects trailing input.</summary>
    public IReadOnlyList<Statement> ParseProgram()
    {
        var statements = ParseSequence(TokenKind.End);
        if (statements.Count == 0)
            throw new WpParseException("Введите хотя бы один оператор", Current.Position);
        Expect(TokenKind.End, "Конец программы");
        return statements;
    }

    /// <summary>Parses one complete Boolean postcondition.</summary>
    public BooleanExpression ParsePostcondition()
    {
        var result = ParseOr();
        Expect(TokenKind.End, "После постусловия обнаружен лишний текст");
        return result;
    }

    /// <summary>Reads semicolon-separated statements up to a closing brace or end of input.</summary>
    private List<Statement> ParseSequence(TokenKind end)
    {
        var result = new List<Statement>();
        while (Current.Kind != end && Current.Kind != TokenKind.End)
        {
            result.Add(ParseStatement());
            if (Match(TokenKind.Semicolon))
            {
                while (Match(TokenKind.Semicolon)) { }
            }
            else if (Current.Kind != end && Current.Kind != TokenKind.End)
            {
                throw new WpParseException("Между операторами нужна точка с запятой", Current.Position);
            }
        }
        return result;
    }

    /// <summary>Parses an assignment or a conditional with a mandatory else branch.</summary>
    private Statement ParseStatement()
    {
        if (Match(TokenKind.If))
        {
            Expect(TokenKind.LeftParen, "После if нужна открывающая скобка");
            var condition = ParseOr();
            Expect(TokenKind.RightParen, "После условия if нужна закрывающая скобка");
            Expect(TokenKind.LeftBrace, "После условия if нужна открывающая фигурная скобка");
            var thenBranch = ParseSequence(TokenKind.RightBrace);
            Expect(TokenKind.RightBrace, "Не закрыта ветка if");
            Expect(TokenKind.Else, "Нужна полная форма if с веткой else");
            Expect(TokenKind.LeftBrace, "После else нужна открывающая фигурная скобка");
            var elseBranch = ParseSequence(TokenKind.RightBrace);
            Expect(TokenKind.RightBrace, "Не закрыта ветка else");
            return new IfStatement(condition, thenBranch, elseBranch);
        }

        var name = Expect(TokenKind.Identifier, "Ожидается имя переменной слева от :=");
        Expect(TokenKind.Assign, "После имени переменной нужен оператор :=");
        return new AssignmentStatement(name.Text, ParseAddSubtract());
    }

    /// <summary>Parses OR at the lowest Boolean precedence level.</summary>
    private BooleanExpression ParseOr()
    {
        var left = ParseAnd();
        while (Match(TokenKind.Or))
            left = new BooleanBinaryExpression(left, BooleanOperator.Or, ParseAnd());
        return left;
    }

    /// <summary>Parses AND before combining terms with OR.</summary>
    private BooleanExpression ParseAnd()
    {
        var left = ParseNot();
        while (Match(TokenKind.And))
            left = new BooleanBinaryExpression(left, BooleanOperator.And, ParseNot());
        return left;
    }

    /// <summary>Parses negation, literals, grouped conditions, or comparisons.</summary>
    private BooleanExpression ParseNot()
    {
        if (Match(TokenKind.Not))
            return new NotExpression(ParseNot());

        if (Current.Kind == TokenKind.Identifier &&
            (Current.Text == "true" || Current.Text == "false"))
            return new BooleanConstant(Advance().Text == "true");

        // A leading '(' can start either a Boolean group or the arithmetic
        // left-hand side of a comparison such as (x + 1) > 0.
        if (Current.Kind == TokenKind.LeftParen)
        {
            var saved = index;
            try { return ParseComparison(); }
            catch (WpParseException) { index = saved; }

            Advance();
            var grouped = ParseOr();
            Expect(TokenKind.RightParen, "Не закрыта скобка в условии");
            return grouped;
        }

        return ParseComparison();
    }

    /// <summary>Requires a comparison operator between arithmetic expressions.</summary>
    private BooleanExpression ParseComparison()
    {
        var left = ParseAddSubtract();
        var operation = Current.Kind switch
        {
            TokenKind.Equal => ComparisonOperator.Equal,
            TokenKind.NotEqual => ComparisonOperator.NotEqual,
            TokenKind.Less => ComparisonOperator.Less,
            TokenKind.LessOrEqual => ComparisonOperator.LessOrEqual,
            TokenKind.Greater => ComparisonOperator.Greater,
            TokenKind.GreaterOrEqual => ComparisonOperator.GreaterOrEqual,
            _ => throw new WpParseException("Ожидается знак сравнения (=, !=, <, <=, >, >=)", Current.Position)
        };
        Advance();
        return new ComparisonExpression(left, operation, ParseAddSubtract());
    }

    /// <summary>Parses left-associative addition and subtraction.</summary>
    private ArithmeticExpression ParseAddSubtract()
    {
        var left = ParseMultiplyDivide();
        while (Current.Kind is TokenKind.Plus or TokenKind.Minus)
        {
            var operation = Advance().Kind == TokenKind.Plus
                ? ArithmeticOperator.Add : ArithmeticOperator.Subtract;
            left = new ArithmeticBinaryExpression(left, operation, ParseMultiplyDivide());
        }
        return left;
    }

    /// <summary>Parses multiplication and division before addition and subtraction.</summary>
    private ArithmeticExpression ParseMultiplyDivide()
    {
        var left = ParseUnary();
        while (Current.Kind is TokenKind.Star or TokenKind.Slash)
        {
            var operation = Advance().Kind == TokenKind.Star
                ? ArithmeticOperator.Multiply : ArithmeticOperator.Divide;
            left = new ArithmeticBinaryExpression(left, operation, ParseUnary());
        }
        return left;
    }

    /// <summary>Applies leading signs to an arithmetic primary expression.</summary>
    private ArithmeticExpression ParseUnary()
    {
        if (Match(TokenKind.Plus)) return ParseUnary();
        if (Match(TokenKind.Minus)) return new NegativeExpression(ParseUnary());
        return ParseArithmeticPrimary();
    }

    /// <summary>Parses numbers, variables, supported functions, and arithmetic groups.</summary>
    private ArithmeticExpression ParseArithmeticPrimary()
    {
        if (Current.Kind == TokenKind.Number)
        {
            var token = Advance();
            if (!decimal.TryParse(token.Text, NumberStyles.AllowDecimalPoint,
                                  CultureInfo.InvariantCulture, out var number))
                throw new WpParseException("Число вне допустимого диапазона", token.Position);
            return new NumberExpression(number);
        }

        if (Current.Kind == TokenKind.Identifier)
        {
            var name = Advance();
            if (!Match(TokenKind.LeftParen)) return new VariableExpression(name.Text);

            if (name.Text is not ("abs" or "sqrt"))
                throw new WpParseException($"Функция {name.Text} не поддерживается", name.Position);
            var argument = ParseAddSubtract();
            Expect(TokenKind.RightParen, "Не закрыта скобка вызова функции");
            return new FunctionExpression(name.Text, argument);
        }

        if (Match(TokenKind.LeftParen))
        {
            var grouped = ParseAddSubtract();
            Expect(TokenKind.RightParen, "Не закрыта скобка в выражении");
            return grouped;
        }

        throw new WpParseException("Ожидается число, переменная или выражение в скобках", Current.Position);
    }

    /// <summary>Gets the next token without consuming it.</summary>
    private Token Current => tokens[index];

    /// <summary>Consumes and returns the next token.</summary>
    private Token Advance() => tokens[index++];

    /// <summary>Consumes the next token only when it has the requested kind.</summary>
    private bool Match(TokenKind kind)
    {
        if (Current.Kind != kind) return false;
        Advance();
        return true;
    }

    /// <summary>Consumes a required token or reports its source position.</summary>
    private Token Expect(TokenKind kind, string message)
    {
        if (Current.Kind != kind) throw new WpParseException(message, Current.Position);
        return Advance();
    }

    /// <summary>Recognizes keywords, decimal literals, operators, and Unicode aliases.</summary>
    private static IEnumerable<Token> Tokenize(string source)
    {
        for (var i = 0; i < source.Length;)
        {
            if (char.IsWhiteSpace(source[i])) { i++; continue; }
            var start = i;
            var c = source[i];

            if (char.IsLetter(c) || c == '_')
            {
                i++;
                while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '_')) i++;
                var word = source[start..i];
                yield return new Token(word switch
                {
                    "if" => TokenKind.If,
                    "else" => TokenKind.Else,
                    _ => TokenKind.Identifier
                }, word, start);
                continue;
            }

            if (char.IsDigit(c))
            {
                i++;
                while (i < source.Length && char.IsDigit(source[i])) i++;
                if (i + 1 < source.Length && source[i] == '.' && char.IsDigit(source[i + 1]))
                {
                    i++;
                    while (i < source.Length && char.IsDigit(source[i])) i++;
                }
                yield return new Token(TokenKind.Number, source[start..i], start);
                continue;
            }

            if (i + 1 < source.Length)
            {
                var pair = source.Substring(i, 2);
                var pairKind = pair switch
                {
                    ":=" => TokenKind.Assign,
                    ">=" => TokenKind.GreaterOrEqual,
                    "<=" => TokenKind.LessOrEqual,
                    "==" => TokenKind.Equal,
                    "!=" => TokenKind.NotEqual,
                    "&&" => TokenKind.And,
                    "||" => TokenKind.Or,
                    _ => TokenKind.End
                };
                if (pairKind != TokenKind.End)
                {
                    i += 2;
                    yield return new Token(pairKind, pair, start);
                    continue;
                }
            }

            var kind = c switch
            {
                ';' => TokenKind.Semicolon,
                '{' => TokenKind.LeftBrace,
                '}' => TokenKind.RightBrace,
                '(' => TokenKind.LeftParen,
                ')' => TokenKind.RightParen,
                '+' => TokenKind.Plus,
                '-' => TokenKind.Minus,
                '*' => TokenKind.Star,
                '/' => TokenKind.Slash,
                '>' => TokenKind.Greater,
                '<' => TokenKind.Less,
                '=' => TokenKind.Equal,
                '!' or '¬' => TokenKind.Not,
                '≥' => TokenKind.GreaterOrEqual,
                '≤' => TokenKind.LessOrEqual,
                '≠' => TokenKind.NotEqual,
                '∧' => TokenKind.And,
                '∨' => TokenKind.Or,
                '≔' => TokenKind.Assign,
                _ => throw new WpParseException($"Недопустимый символ '{c}'", start)
            };
            i++;
            yield return new Token(kind, c.ToString(), start);
        }

        yield return new Token(TokenKind.End, string.Empty, source.Length);
    }
}
