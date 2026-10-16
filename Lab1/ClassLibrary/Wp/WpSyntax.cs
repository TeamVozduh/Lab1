using System.Globalization;

namespace ClassLibrary.Wp;

public sealed class WpParseException : FormatException
{
    public int Position { get; }

    public WpParseException(string message, int position)
        : base($"{message} (позиция {position + 1}).")
    {
        Position = position;
    }
}

internal abstract record ArithmeticExpression;
internal sealed record NumberExpression(decimal Value) : ArithmeticExpression;
internal sealed record VariableExpression(string Name) : ArithmeticExpression;
internal sealed record NegativeExpression(ArithmeticExpression Operand) : ArithmeticExpression;
internal sealed record ArithmeticBinaryExpression(
    ArithmeticExpression Left, ArithmeticOperator Operator, ArithmeticExpression Right) : ArithmeticExpression;
internal sealed record FunctionExpression(string Name, ArithmeticExpression Argument) : ArithmeticExpression;

internal enum ArithmeticOperator { Add, Subtract, Multiply, Divide }

internal abstract record BooleanExpression;
internal sealed record BooleanConstant(bool Value) : BooleanExpression;
internal sealed record ComparisonExpression(
    ArithmeticExpression Left, ComparisonOperator Operator, ArithmeticExpression Right) : BooleanExpression;
internal sealed record NotExpression(BooleanExpression Operand) : BooleanExpression;
internal sealed record BooleanBinaryExpression(
    BooleanExpression Left, BooleanOperator Operator, BooleanExpression Right) : BooleanExpression;

internal enum ComparisonOperator { Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual }
internal enum BooleanOperator { And, Or }

internal abstract record Statement;
internal sealed record AssignmentStatement(string Variable, ArithmeticExpression Value) : Statement;
internal sealed record IfStatement(
    BooleanExpression Condition, IReadOnlyList<Statement> ThenBranch, IReadOnlyList<Statement> ElseBranch) : Statement;

internal enum TokenKind
{
    End, Identifier, Number, If, Else, Assign, Semicolon, LeftBrace, RightBrace,
    LeftParen, RightParen, Plus, Minus, Star, Slash, Greater, GreaterOrEqual,
    Less, LessOrEqual, Equal, NotEqual, Not, And, Or
}

internal readonly record struct Token(TokenKind Kind, string Text, int Position);

internal sealed class WpParser
{
    private readonly List<Token> tokens;
    private int index;

    public WpParser(string source)
    {
        tokens = Tokenize(source).ToList();
    }

    public IReadOnlyList<Statement> ParseProgram()
    {
        var statements = ParseSequence(TokenKind.End);
        if (statements.Count == 0)
            throw new WpParseException("Введите хотя бы один оператор", Current.Position);
        Expect(TokenKind.End, "Конец программы");
        return statements;
    }

    public BooleanExpression ParsePostcondition()
    {
        var result = ParseOr();
        Expect(TokenKind.End, "После постусловия обнаружен лишний текст");
        return result;
    }

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

    private BooleanExpression ParseOr()
    {
        var left = ParseAnd();
        while (Match(TokenKind.Or))
            left = new BooleanBinaryExpression(left, BooleanOperator.Or, ParseAnd());
        return left;
    }

    private BooleanExpression ParseAnd()
    {
        var left = ParseNot();
        while (Match(TokenKind.And))
            left = new BooleanBinaryExpression(left, BooleanOperator.And, ParseNot());
        return left;
    }

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

    private ArithmeticExpression ParseUnary()
    {
        if (Match(TokenKind.Plus)) return ParseUnary();
        if (Match(TokenKind.Minus)) return new NegativeExpression(ParseUnary());
        return ParseArithmeticPrimary();
    }

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

    private Token Current => tokens[index];

    private Token Advance() => tokens[index++];

    private bool Match(TokenKind kind)
    {
        if (Current.Kind != kind) return false;
        Advance();
        return true;
    }

    private Token Expect(TokenKind kind, string message)
    {
        if (Current.Kind != kind) throw new WpParseException(message, Current.Position);
        return Advance();
    }

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
