using ClassLibrary.Wp;
using NUnit.Framework;

namespace UnitTests;

[TestFixture]
internal class WpEngineTests
{
    [TestCase("x := x + 1", "x > 5", "x > 4")]
    [TestCase("x := x - 1", "x > 5", "x > 6")]
    [TestCase("x := x * 2", "x > 10", "x * 2 > 10")]
    [TestCase("x := 7", "x == 7", "true")]
    [TestCase("x := 7", "x == 8", "false")]
    [TestCase("x := y", "x > 3", "y > 3")]
    [TestCase("x := y; y := 4", "x == 4", "y == 4")]
    [TestCase("x := x + 1; x := x * 2", "x > 10", "(x + 1) * 2 > 10")]
    public void Calculate_ComputesWeakestPrecondition(string program, string post, string expectedPre)
    {
        var result = WpEngine.Calculate(program, post);
        Assert.That(result.WeakestPrecondition, Is.EqualTo(expectedPre));
    }

    [TestCase("x := 1 / y", "x > 0", "y != 0")]
    [TestCase("x := sqrt(x)", "x >= 0", "x >= 0")]
    public void Calculate_IncludesDefinednessConditions(string program, string post, string expectedPart)
    {
        var result = WpEngine.Calculate(program, post);
        Assert.That(result.WeakestPrecondition, Does.Contain(expectedPart));
    }

    [Test]
    public void Calculate_IfElse_UsesBothBranches()
    {
        var result = WpEngine.Calculate("if (x > 0) { y := x } else { y := x + 1 }", "y > 0");
        Assert.That(result.WeakestPrecondition, Does.Contain("x > 0"));
        Assert.That(result.WeakestPrecondition, Does.Contain("x <= 0"));
    }

    [Test]
    public void Calculate_NestedIfElse_ReturnsCondition()
    {
        var result = WpEngine.Calculate(
            "if (x > 0) { if (y > 0) { z := 1 } else { z := 2 } } else { z := 3 }", "z == 1");
        Assert.That(result.WeakestPrecondition, Does.Contain("x > 0"));
        Assert.That(result.WeakestPrecondition, Does.Contain("y > 0"));
    }

    [Test]
    public void Calculate_EmptyBranches_AreAllowed()
    {
        var result = WpEngine.Calculate("if (x > 0) { } else { }", "x > 0");
        Assert.That(result.WeakestPrecondition, Is.Not.Empty);
    }

    [Test]
    public void Calculate_ReturnsPostHumanReadableTraceAndTriad()
    {
        var result = WpEngine.Calculate("x := x + 1", "x > 5");
        Assert.That(result.Post, Is.EqualTo("x > 5"));
        Assert.That(result.PostHumanReadable, Does.Contain("После выполнения"));
        Assert.That(result.WeakestPreconditionHumanReadable, Does.Contain("До выполнения"));
        Assert.That(result.Trace, Does.Contain("Цель (Post)"));
        Assert.That(result.Trace, Does.Contain("Итоговое wp"));
        Assert.That(result.Triad, Does.Contain("{ x > 4 }"));
        Assert.That(result.Triad, Does.Contain("{ x > 5 }"));
    }

    [TestCase("", "x > 0")]
    [TestCase("x := 1", "")]
    public void Calculate_EmptyProgramOrPost_ThrowsParseException(string program, string post)
    {
        Assert.Throws<WpParseException>(() => WpEngine.Calculate(program, post));
    }

    [Test]
    public void Calculate_NullProgram_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => WpEngine.Calculate(null!, "true"));
    }

    [Test]
    public void Calculate_NullPost_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => WpEngine.Calculate("x := 1", null!));
    }

    [Test]
    public void DescribePostcondition_ReturnsHumanReadableDescription()
    {
        Assert.That(WpEngine.DescribePostcondition("x > 2"), Does.Contain("После выполнения"));
        Assert.That(WpEngine.DescribePostcondition("true"), Does.Contain("всегда"));
    }

    [Test]
    public void DescribePostcondition_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => WpEngine.DescribePostcondition(null!));
    }

    [TestCase("x :=", "true")]
    [TestCase("if (x > 0) { x := 1 }", "true")]
    [TestCase("x := 1 y := 2", "true")]
    [TestCase("x := 1", "x >")]
    [TestCase("x := 1", "x > 0 extra")]
    public void Calculate_InvalidSyntax_ThrowsWpParseException(string program, string post)
    {
        Assert.Throws<WpParseException>(() => WpEngine.Calculate(program, post));
    }
}
