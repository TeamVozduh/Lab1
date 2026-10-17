using ClassLibrary.Wp;
using NUnit.Framework;

namespace UnitTests;

[TestFixture]
internal class WpLogicTests
{
    private static BooleanExpression C(bool value)
    {
        return new BooleanConstant(value);
    }
    private static ArithmeticExpression N(decimal value)
    {
        return new NumberExpression(value);
    }
    private static ArithmeticExpression V(string name)
    {
        return new VariableExpression(name);
    }
    private static BooleanExpression Compare(ArithmeticExpression left, ComparisonOperator op, ArithmeticExpression right)
    {
        return new ComparisonExpression(left, op, right);
    }

    [TestCase(ComparisonOperator.Equal, true)]
    [TestCase(ComparisonOperator.NotEqual, false)]
    [TestCase(ComparisonOperator.Less, false)]
    [TestCase(ComparisonOperator.LessOrEqual, true)]
    [TestCase(ComparisonOperator.Greater, false)]
    [TestCase(ComparisonOperator.GreaterOrEqual, true)]
    public void Compare_EqualOperands_SimplifiesCorrectly(ComparisonOperator op, bool expected)
    {
        Assert.That(WpLogic.Compare(V("x"), op, V("x")), Is.EqualTo(C(expected)));
    }

    [TestCase(ComparisonOperator.Equal, false)]
    [TestCase(ComparisonOperator.NotEqual, true)]
    [TestCase(ComparisonOperator.Less, true)]
    [TestCase(ComparisonOperator.LessOrEqual, true)]
    [TestCase(ComparisonOperator.Greater, false)]
    [TestCase(ComparisonOperator.GreaterOrEqual, false)]
    public void Compare_ConstantOperands_Evaluates(ComparisonOperator op, bool expected)
    {
        Assert.That(WpLogic.Compare(N(1), op, N(2)), Is.EqualTo(C(expected)));
    }

    [Test]
    public void Compare_ShiftsConstantOffset()
    {
        var expression = new ArithmeticBinaryExpression(V("x"), ArithmeticOperator.Add, N(10));
        Assert.That(WpLogic.Format(WpLogic.Compare(expression, ComparisonOperator.Greater, N(15))), Is.EqualTo("x > 5"));
    }

    [TestCase(true, true, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    public void And_ConstantTruthTable(bool left, bool right, bool expected)
    {
        Assert.That(WpLogic.And(C(left), C(right)), Is.EqualTo(C(expected)));
    }

    [TestCase(true, true, true)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    [TestCase(false, false, false)]
    public void Or_ConstantTruthTable(bool left, bool right, bool expected)
    {
        Assert.That(WpLogic.Or(C(left), C(right)), Is.EqualTo(C(expected)));
    }

    [Test]
    public void And_Contradiction_IsFalse()
    {
        var condition = Compare(V("x"), ComparisonOperator.Greater, N(0));
        Assert.That(WpLogic.And(condition, WpLogic.Not(condition)), Is.EqualTo(C(false)));
    }

    [Test]
    public void Or_Tautology_IsTrue()
    {
        var condition = Compare(V("x"), ComparisonOperator.Greater, N(0));
        Assert.That(WpLogic.Or(condition, WpLogic.Not(condition)), Is.EqualTo(C(true)));
    }

    [TestCase(ComparisonOperator.Equal, ComparisonOperator.NotEqual)]
    [TestCase(ComparisonOperator.NotEqual, ComparisonOperator.Equal)]
    [TestCase(ComparisonOperator.Less, ComparisonOperator.GreaterOrEqual)]
    [TestCase(ComparisonOperator.LessOrEqual, ComparisonOperator.Greater)]
    [TestCase(ComparisonOperator.Greater, ComparisonOperator.LessOrEqual)]
    [TestCase(ComparisonOperator.GreaterOrEqual, ComparisonOperator.Less)]
    public void Not_ComparisonInvertsOperator(ComparisonOperator source, ComparisonOperator expected)
    {
        var condition = Compare(V("x"), source, N(0));
        var negated = WpLogic.Not(condition);
        Assert.That(negated, Is.TypeOf<ComparisonExpression>());
        Assert.That(((ComparisonExpression)negated).Operator, Is.EqualTo(expected));
    }

    [Test]
    public void Not_DoubleNegationReturnsOperand()
    {
        var condition = Compare(V("x"), ComparisonOperator.Greater, N(0));
        Assert.That(WpLogic.Not(WpLogic.Not(condition)), Is.EqualTo(condition));
    }

    [TestCase(2, ArithmeticOperator.Add, 3, "5")]
    [TestCase(7, ArithmeticOperator.Subtract, 4, "3")]
    [TestCase(3, ArithmeticOperator.Multiply, 4, "12")]
    public void Simplify_ConstantArithmetic(decimal left, ArithmeticOperator op, decimal right, string expected)
    {
        var expression = new ArithmeticBinaryExpression(N(left), op, N(right));
        Assert.That(WpLogic.Format(WpLogic.Simplify(expression)), Is.EqualTo(expected));
    }

    [TestCase(ArithmeticOperator.Add, "x")]
    [TestCase(ArithmeticOperator.Subtract, "x")]
    [TestCase(ArithmeticOperator.Multiply, "0")]
    public void Simplify_ArithmeticIdentities(ArithmeticOperator op, string expected)
    {
        ArithmeticExpression expression = new ArithmeticBinaryExpression(V("x"), op, N(0));
        Assert.That(WpLogic.Format(WpLogic.Simplify(expression)), Is.EqualTo(expected));
    }

    [Test]
    public void Simplify_DivisionDoesNotRoundDecimal()
    {
        var expression = new ArithmeticBinaryExpression(N(1), ArithmeticOperator.Divide, N(3));
        Assert.That(WpLogic.Format(WpLogic.Simplify(expression)), Is.EqualTo("1 / 3"));
    }

    [Test]
    public void Simplify_OverflowKeepsExpressionSymbolic()
    {
        var expression = new ArithmeticBinaryExpression(N(decimal.MaxValue), ArithmeticOperator.Add, N(1));
        Assert.That(WpLogic.Format(WpLogic.Simplify(expression)), Is.EqualTo("79228162514264337593543950335 + 1"));
    }

    [Test]
    public void Defined_DivisionRequiresNonzeroDenominator()
    {
        var expression = new ArithmeticBinaryExpression(V("x"), ArithmeticOperator.Divide, V("y"));
        Assert.That(WpLogic.Format(WpLogic.Defined(expression)), Is.EqualTo("y != 0"));
    }

    [Test]
    public void Defined_SqrtRequiresNonnegativeArgument()
    {
        var expression = new FunctionExpression("sqrt", V("x"));
        Assert.That(WpLogic.Format(WpLogic.Defined(expression)), Is.EqualTo("x >= 0"));
    }

    [Test]
    public void Defined_AbsArgumentIsDefinedWhenItsArgumentIs()
    {
        var expression = new FunctionExpression("abs", V("x"));
        Assert.That(WpLogic.Format(WpLogic.Defined(expression)), Is.EqualTo("true"));
    }

    [Test]
    public void Defined_PostconditionWithDivisionIncludesDefinedness()
    {
        var expression = Compare(new ArithmeticBinaryExpression(N(1), ArithmeticOperator.Divide, V("x")),
            ComparisonOperator.Greater, N(0));
        Assert.That(WpLogic.Format(WpLogic.Defined(expression)), Is.EqualTo("x != 0"));
    }

    [Test]
    public void Substitute_ReplacesEveryOccurrenceInComparison()
    {
        var condition = Compare(new ArithmeticBinaryExpression(V("x"), ArithmeticOperator.Add, V("x")),
            ComparisonOperator.Greater, N(4));
        var result = WpLogic.Substitute(condition, "x", N(3));
        Assert.That(WpLogic.Format(result), Is.EqualTo("true"));
    }

    [Test]
    public void Substitute_DoesNotReplaceOtherVariables()
    {
        var condition = Compare(V("y"), ComparisonOperator.Greater, N(0));
        Assert.That(WpLogic.Format(WpLogic.Substitute(condition, "x", N(3))), Is.EqualTo("y > 0"));
    }

    [Test]
    public void Format_ArithmeticPreservesPrecedence()
    {
        ArithmeticExpression expression = new ArithmeticBinaryExpression(V("x"), ArithmeticOperator.Multiply,
            new ArithmeticBinaryExpression(V("y"), ArithmeticOperator.Add, N(1)));
        Assert.That(WpLogic.Format(expression), Is.EqualTo("x * (y + 1)"));
    }

    [Test]
    public void Format_BooleanPreservesPrecedence()
    {
        BooleanExpression expression = new BooleanBinaryExpression(
            Compare(V("x"), ComparisonOperator.Greater, N(0)), BooleanOperator.Or,
            new BooleanBinaryExpression(Compare(V("y"), ComparisonOperator.Greater, N(0)), BooleanOperator.And,
                Compare(V("z"), ComparisonOperator.Greater, N(0))));
        Assert.That(WpLogic.Format(expression), Is.EqualTo("x > 0 || y > 0 && z > 0"));
    }

    [Test]
    public void Defined_AndUsesShortCircuitSemantics()
    {
        var left = Compare(V("x"), ComparisonOperator.NotEqual, N(0));
        var division = new ArithmeticBinaryExpression(N(1), ArithmeticOperator.Divide, V("x"));
        var right = Compare(division, ComparisonOperator.Greater, N(0));
        var condition = new BooleanBinaryExpression(left, BooleanOperator.And, right);
        Assert.That(WpLogic.Format(WpLogic.Defined(condition)), Is.EqualTo("true"));
    }

    [Test]
    public void Defined_OrUsesShortCircuitSemantics()
    {
        var left = Compare(V("x"), ComparisonOperator.Equal, N(0));
        var division = new ArithmeticBinaryExpression(N(1), ArithmeticOperator.Divide, V("x"));
        var right = Compare(division, ComparisonOperator.Greater, N(0));
        var condition = new BooleanBinaryExpression(left, BooleanOperator.Or, right);
        Assert.That(WpLogic.Format(WpLogic.Defined(condition)), Is.EqualTo("true"));
    }

    [Test]
    public void Simplify_NestedNegationCancels()
    {
        ArithmeticExpression expression = new NegativeExpression(new NegativeExpression(V("x")));
        Assert.That(WpLogic.Format(WpLogic.Simplify(expression)), Is.EqualTo("x"));
    }

    [Test]
    public void Substitute_ReplacesInsideFunctionArguments()
    {
        var condition = Compare(new FunctionExpression("sqrt", V("x")), ComparisonOperator.GreaterOrEqual, N(0));
        var result = WpLogic.Substitute(condition, "x", N(4));
        Assert.That(WpLogic.Format(result), Is.EqualTo("sqrt(4) >= 0"));
    }

    [Test]
    public void Describe_ContainsReadableComparison()
    {
        Assert.That(WpLogic.Describe(Compare(V("x"), ComparisonOperator.Greater, N(0))), Is.EqualTo("x больше 0"));
    }

    [Test]
    public void Describe_TrueAndFalseConstants()
    {
        Assert.That(WpLogic.Describe(C(true)), Does.Contain("всегда"));
        Assert.That(WpLogic.Describe(C(false)), Does.Contain("невыполнимо"));
    }

    [Test]
    public void Simplify_AdditionWithZero_ReturnsVariable()
    {
        ArithmeticExpression expression = new ArithmeticBinaryExpression(
            V("x"),
            ArithmeticOperator.Add,
            N(0));

        Assert.That(
            WpLogic.Format(WpLogic.Simplify(expression)),
            Is.EqualTo("x"));
    }

    [Test]
    public void Simplify_SubtractionWithZero_ReturnsVariable()
    {
        ArithmeticExpression expression = new ArithmeticBinaryExpression(
            V("x"),
            ArithmeticOperator.Subtract,
            N(0));

        Assert.That(
            WpLogic.Format(WpLogic.Simplify(expression)),
            Is.EqualTo("x"));
    }

    [Test]
    public void Simplify_MultiplyByOne_ReturnsVariable()
    {
        ArithmeticExpression expression = new ArithmeticBinaryExpression(
            V("x"),
            ArithmeticOperator.Multiply,
            N(1));

        Assert.That(
            WpLogic.Format(WpLogic.Simplify(expression)),
            Is.EqualTo("x"));
    }

    [Test]
    public void Simplify_ZeroMultipliedByVariable_ReturnsZero()
    {
        ArithmeticExpression expression = new ArithmeticBinaryExpression(
            N(0),
            ArithmeticOperator.Multiply,
            V("x"));

        Assert.That(
            WpLogic.Format(WpLogic.Simplify(expression)),
            Is.EqualTo("0"));
    }

    [Test]
    public void Simplify_CombinesAdditionOffsets()
    {
        ArithmeticExpression expression = new ArithmeticBinaryExpression(
            new ArithmeticBinaryExpression(
                V("x"),
                ArithmeticOperator.Add,
                N(2)),
            ArithmeticOperator.Add,
            N(3));

        Assert.That(
            WpLogic.Format(WpLogic.Simplify(expression)),
            Is.EqualTo("x + 5"));
    }

    [Test]
    public void Simplify_CombinesSubtractionOffsets()
    {
        ArithmeticExpression expression = new ArithmeticBinaryExpression(
            new ArithmeticBinaryExpression(
                V("x"),
                ArithmeticOperator.Subtract,
                N(5)),
            ArithmeticOperator.Add,
            N(2));

        Assert.That(
            WpLogic.Format(WpLogic.Simplify(expression)),
            Is.EqualTo("x - 3"));
    }

    [Test]
    public void Simplify_NegativeNumber_ReturnsNegativeValue()
    {
        ArithmeticExpression expression = new NegativeExpression(N(5));

        Assert.That(
            WpLogic.Format(WpLogic.Simplify(expression)),
            Is.EqualTo("-5"));
    }

    [Test]
    public void Simplify_DoubleNegation_ReturnsOriginalVariable()
    {
        ArithmeticExpression expression = new NegativeExpression(
            new NegativeExpression(V("x")));

        Assert.That(
            WpLogic.Format(WpLogic.Simplify(expression)),
            Is.EqualTo("x"));
    }
}
