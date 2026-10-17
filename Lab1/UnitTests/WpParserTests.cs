using ClassLibrary.Wp;
using NUnit.Framework;

namespace UnitTests;

[TestFixture]
internal class WpParserTests
{
    [TestCase("x := 1", 1)]
    [TestCase("x := 1; y := 2", 2)]
    [TestCase("x := 1;;; y := 2;", 2)]
    [TestCase("if (x > 0) { y := 1 } else { y := 2 }", 1)]
    public void ParseProgram_ValidPrograms_ReturnExpectedStatementCount(string source, int count)
    {
        Assert.That(new WpParser(source).ParseProgram(), Has.Count.EqualTo(count));
    }

    [Test]
    public void ParseProgram_AssignmentProducesAssignmentNode()
    {
        Assert.That(new WpParser("score := x + 2").ParseProgram()[0], Is.TypeOf<AssignmentStatement>());
    }

    [Test]
    public void ParseProgram_IfProducesIfNode()
    {
        Assert.That(new WpParser("if (x > 0) { x := 1 } else { x := 2 }").ParseProgram()[0], Is.TypeOf<IfStatement>());
    }

    [TestCase("")]
    [TestCase("   ")]
    public void ParseProgram_EmptyProgram_Throws(string source)
    {
        Assert.Throws<WpParseException>(() => new WpParser(source).ParseProgram());
    }

    [TestCase("x = 1")]
    [TestCase("x 1")]
    [TestCase("x := 1 y := 2")]
    [TestCase("if x > 0 { x := 1 } else { x := 2 }")]
    [TestCase("if (x > 0) x := 1 else { x := 2 }")]
    [TestCase("if (x > 0) { x := 1 }")]
    [TestCase("if (x > 0) { x := 1 } else x := 2")]
    [TestCase("x := (1 + 2")]
    [TestCase("x := 1 @ 2")]
    [TestCase("x := unknown(2)")]
    [TestCase("x := 1..2")]
    public void ParseProgram_InvalidPrograms_ThrowsWpParseException(string source)
    {
        Assert.Throws<WpParseException>(() => new WpParser(source).ParseProgram());
    }

    [TestCase("x > 1")]
    [TestCase("x + 1 >= 2")]
    [TestCase("!(x == 0)")]
    [TestCase("x > 0 && y < 3 || z == 1")]
    [TestCase("(x + 1) > 0")]
    [TestCase("(x > 0 || y > 0) && z != 0")]
    [TestCase("sqrt(x) >= 0")]
    [TestCase("abs(-2) == 2")]
    [TestCase("true")]
    [TestCase("false")]
    public void ParsePostcondition_ValidConditions_ReturnsExpression(string source)
    {
        Assert.That(new WpParser(source).ParsePostcondition(), Is.Not.Null);
    }

    [TestCase("")]
    [TestCase("x")]
    [TestCase("x >")]
    [TestCase("> 1")]
    [TestCase("x > 1 trailing")]
    [TestCase("x > 1 @")]
    [TestCase("unknown(1) == 1")]
    [TestCase("(x > 1")]
    [TestCase("sqrt(1 == 1)")]
    public void ParsePostcondition_InvalidConditions_ThrowsWpParseException(string source)
    {
        Assert.Throws<WpParseException>(() => new WpParser(source).ParsePostcondition());
    }

    [TestCase("x := 1", 0)]
    [TestCase("x := 1.25", 0)]
    [TestCase("x := -2", 0)]
    [TestCase("x := +2", 0)]
    [TestCase("x := a * (b + 2)", 0)]
    [TestCase("x := abs(-1) + sqrt(4)", 0)]
    [TestCase("x ≔ 2", 0)]
    public void ParseProgram_AcceptsArithmeticSyntax(string source, int _)
    {
        Assert.That(new WpParser(source).ParseProgram(), Has.Count.EqualTo(1));
    }

    [TestCase("x := 999999999999999999999999999999999999999999999999999999")]
    public void ParseProgram_NumberOutOfDecimalRange_ThrowsWpParseException(string source)
    {
        Assert.Throws<WpParseException>(() => new WpParser(source).ParseProgram());
    }

    [TestCase("x ≥ 2", "x >= 2")]
    [TestCase("x ≤ 2", "x <= 2")]
    [TestCase("x ≠ 2", "x != 2")]
    [TestCase("x > 0 ∧ y > 0", "x > 0 && y > 0")]
    public void ParsePostcondition_UnicodeOperatorsAreRecognized(string source, string _)
    {
        Assert.That(new WpParser(source).ParsePostcondition(), Is.Not.Null);
    }

    [Test]
    public void ParsePostcondition_LogicalAndBindsMoreTightlyThanOr()
    {
        var parsed = new WpParser("x > 0 || y > 0 && z > 0").ParsePostcondition();
        Assert.That(parsed, Is.TypeOf<BooleanBinaryExpression>());
        var outer = (BooleanBinaryExpression)parsed;
        Assert.That(outer.Operator, Is.EqualTo(BooleanOperator.Or));
        Assert.That(outer.Right, Is.TypeOf<BooleanBinaryExpression>());
        Assert.That(((BooleanBinaryExpression)outer.Right).Operator, Is.EqualTo(BooleanOperator.And));
    }

    [Test]
    public void ParseProgram_ParsesNestedIf()
    {
        var statements = new WpParser("if (x > 0) { if (y > 0) { z := 1 } else { z := 2 } } else { z := 3 }").ParseProgram();
        Assert.That(statements, Has.Count.EqualTo(1));
        var outer = (IfStatement)statements[0];
        Assert.That(outer.ThenBranch, Has.Count.EqualTo(1));
        Assert.That(outer.ThenBranch[0], Is.TypeOf<IfStatement>());
    }

    [Test]
    public void ParseException_ContainsPosition()
    {
        var exception = Assert.Throws<WpParseException>(() => new WpParser("x := 1 @").ParseProgram());
        Assert.That(exception!.Position, Is.EqualTo(7));
        Assert.That(exception.Message, Does.Contain("позиция"));
    }
}
