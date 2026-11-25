using ClassLibrary.Loops;
using ClassLibrary.Wp;
using Xunit;

namespace LoopTests;

public class LoopSessionTests
{
    /// <summary>Перебираем все малые массивы: это обнаруживает ошибки индекса и строгого сравнения с T.</summary>
    [Theory]
    [InlineData(LoopMode.PrefixSum)]
    [InlineData(LoopMode.CountAboveThreshold)]
    [InlineData(LoopMode.PrefixMax)]
    public void ExhaustiveSmallArrays_PreserveInvariant_PostAndVariant(LoopMode mode)
    {
        for (var n = mode == LoopMode.PrefixMax ? 1 : 0; n <= 5; n++)
        for (var code = 0; code < (int)Math.Pow(3, n); code++)
        for (var threshold = -1; threshold <= 1; threshold++)
        {
            var a = new int[n];
            var rest = code;
            for (var i = 0; i < n; i++) { a[i] = rest % 3 - 1; rest /= 3; }
            var loop = new LoopSession(mode, a, threshold);
            Assert.True(loop.InitializationHolds);
            var previous = loop.Variant;
            while (loop.CanStep)
            {
                var step = loop.Step();
                Assert.True(step.InvariantBefore);
                Assert.True(step.InvariantAfter);
                Assert.True(step.PreservationHolds);
                Assert.True(step.VariantDecreases);
                Assert.Equal(previous - 1, step.VariantAfter);
                Assert.True(step.VariantAfter >= 0);
                previous = step.VariantAfter;
            }
            Assert.False(loop.IsFaulted);
            Assert.True(loop.PostHolds);
            Assert.Equal(n, loop.Index);
            Assert.Equal(0, loop.Variant);
            decimal expected = mode switch
            {
                LoopMode.PrefixSum => a.Sum(x => (decimal)x),
                LoopMode.CountAboveThreshold => a.Count(x => x > threshold),
                _ => a.Max()
            };
            Assert.Equal(expected, loop.Result);
        }
    }

    [Theory]
    [InlineData(LoopMode.PrefixSum, 205)]
    [InlineData(LoopMode.CountAboveThreshold, 3)]
    [InlineData(LoopMode.PrefixMax, 65)]
    public void Demo_HasExpectedResult(LoopMode mode, int expected)
    {
        var loop = new LoopSession(mode, new[] { 12, 48, 30, 65, 8, 42 }, 30);
        while (loop.CanStep) loop.Step();
        Assert.Equal(expected, loop.Result);
        Assert.True(loop.PostHolds);
    }

    [Fact]
    public void Maximum_EmptyArrayIsRejected() => Assert.Throws<ArgumentException>(() => new LoopSession(LoopMode.PrefixMax, Array.Empty<int>()));

    [Fact]
    public void Maximum_SingleNegativeItem_CompletesInInit()
    {
        var loop = new LoopSession(LoopMode.PrefixMax, new[] { -9 });
        Assert.True(loop.PostHolds);
        Assert.Equal(-9, loop.Result);
        Assert.Empty(loop.History);
        Assert.Throws<InvalidOperationException>(() => loop.Step());
    }

    [Fact]
    public void Sum_AvoidsIntOverflow_AndCopiesInput()
    {
        var data = new[] { int.MaxValue, int.MaxValue };
        var loop = new LoopSession(LoopMode.PrefixSum, data);
        data[0] = 0;
        while (loop.CanStep) loop.Step();
        Assert.Equal(2m * int.MaxValue, loop.Result);
    }

    [Fact]
    public void WrongInvariant_IsRejectedAtInitialization()
    {
        var loop = new LoopSession(LoopMode.PrefixSum, new[] { 2 }, invariantFormula: "res == 100");
        Assert.False(loop.InitializationHolds);
        Assert.True(loop.IsFaulted);
        Assert.False(loop.CanStep);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("j + 1")]
    public void NonDecreasingVariant_StopsAfterFirstStep(string variant)
    {
        var loop = new LoopSession(LoopMode.PrefixSum, new[] { 2, 3 }, variantFormula: variant);
        Assert.False(loop.Step().VariantDecreases);
        Assert.True(loop.IsFaulted);
        Assert.False(loop.CanStep);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("0.5")]
    public void InvalidInitialVariant_IsRejected(string variant)
    {
        var loop = new LoopSession(LoopMode.PrefixSum, new[] { 2 }, variantFormula: variant);
        Assert.True(loop.IsFaulted);
    }

    [Fact]
    public void UnknownVariableAndMalformedFormula_AreReported()
    {
        Assert.Throws<ArgumentException>(() => new LoopSession(LoopMode.PrefixSum, new[] { 2 }, invariantFormula: "typo == 0"));
        Assert.Throws<ArgumentException>(() => new LoopSession(LoopMode.PrefixSum, new[] { 2 }, invariantFormula: "true || typo == 0"));
        Assert.Throws<ArgumentException>(() => new LoopSession(LoopMode.PrefixSum, new[] { 2 }, variantFormula: "t0"));
        Assert.Throws<WpParseException>(() => new LoopSession(LoopMode.PrefixSum, new[] { 2 }, invariantFormula: "res =="));
        Assert.Throws<ArgumentException>(() => new LoopSession(LoopMode.PrefixSum, Enumerable.Repeat(1, 1001)));
    }

    [Fact]
    public void Wp_ReusesSubstitution_BranchingAndDefinedness()
    {
        var sum = WpEngine.Calculate(LoopPreset.For(LoopMode.PrefixSum).Body, "res == sum(j)");
        Assert.Contains("sum(j + 1)", sum.WeakestPrecondition);
        Assert.Contains("item(j)", sum.WeakestPrecondition);
        var count = WpEngine.Calculate(LoopPreset.For(LoopMode.CountAboveThreshold).Body, "res == count(j)");
        Assert.Contains("Ветка if", count.Trace);
        Assert.Contains("Ветка else", count.Trace);
        var partial = WpEngine.Calculate("res := item(j) / T; j := j + 1;", "res >= 0");
        Assert.Contains("T != 0", partial.WeakestPrecondition);
        Assert.Contains("j < n", partial.WeakestPrecondition);
        Assert.Contains("j >= 0", partial.WeakestPrecondition);
        var sqrt = WpEngine.Calculate("res := sqrt(item(j));", "res >= 0");
        Assert.Contains("item(j) >= 0", sqrt.WeakestPrecondition);
    }

    [Fact]
    public void EmptyMaximumAndOutOfBounds_AreIncludedInDef()
    {
        Assert.Equal("false", WpEngine.Calculate("j := 0;", "res == maximum(j)").WeakestPrecondition);
        Assert.Contains("0 < n", WpEngine.Calculate("res := item(0);", "res >= 0").WeakestPrecondition);
    }
}
