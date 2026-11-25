using System.Diagnostics;
using ClassLibrary.Wp;

namespace ClassLibrary.Loops;

/// <summary>Неизменяемая запись наблюдений одного реального шага, включая контроль wp и варианта.</summary>
public sealed record LoopStep(int IndexBefore, int IndexAfter, decimal ResultBefore, decimal ResultAfter,
    bool InvariantBefore, bool InvariantAfter, bool PreservationHolds,
    decimal VariantBefore, decimal VariantAfter, bool VariantDecreases);

/// <summary>
/// Независимая от WPF модель init; while(B) { S }. Массив копируется при старте,
/// а тело исполняется из AST ЛР2, исключая расхождение между исполняемым S и его wp.
/// </summary>
public sealed class LoopSession
{
    private readonly Dictionary<string, decimal> state;
    private readonly LoopExpressions evaluator;
    private readonly IReadOnlyList<Statement> body;
    private readonly BooleanExpression invariant;
    private readonly BooleanExpression preservation;
    private readonly ArithmeticExpression variant;
    private readonly List<LoopStep> history = new();

    public LoopPreset Preset { get; }
    public IReadOnlyList<int> Data { get; }
    public IReadOnlyList<LoopStep> History => history.AsReadOnly();
    public string InvariantFormula { get; }
    public string VariantFormula { get; }
    public WpResult PreservationWp { get; }
    public WpResult TerminationWp { get; }
    public WpResult InitializationWp { get; }
    public int Index => (int)state["j"];
    public decimal Result => state["res"];
    public decimal Variant => evaluator.Value(variant, state);
    public bool InvariantHolds => evaluator.Test(invariant, state);
    public bool IsComplete => Index == Data.Count;
    public bool IsFaulted { get; private set; }
    public bool CanStep => !IsComplete && !IsFaulted;
    public bool InitializationHolds { get; }
    public bool PostHolds => IsComplete && evaluator.Test(new WpParser(Preset.Post).ParsePostcondition(), state);
    public string Obligations => Preset.Obligations(InvariantFormula, VariantFormula,
        PreservationWp.WeakestPrecondition, TerminationWp.WeakestPrecondition) +
        "\nwp(init, Inv) = " + InitializationWp.WeakestPrecondition;

    /// <summary>Проверяет вход, разбирает пользовательские формулы и выполняет явную инициализацию.</summary>
    public LoopSession(LoopMode mode, IEnumerable<int> data, int threshold = 30,
        string? invariantFormula = null, string? variantFormula = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        Preset = LoopPreset.For(mode);
        var snapshot = data.Take(1001).ToArray();
        if (snapshot.Length > 1000) throw new ArgumentException("Допустимо не более 1000 поездок.");
        if (mode == LoopMode.PrefixMax && snapshot.Length == 0)
            throw new ArgumentException("Для максимума нужна хотя бы одна поездка: у пустого массива нет максимума.");
        Data = Array.AsReadOnly(snapshot);
        evaluator = new LoopExpressions(Data, threshold);
        state = new Dictionary<string, decimal> { ["n"] = Data.Count, ["j"] = 0, ["res"] = 0,
            ["T"] = threshold, ["t0"] = 0 };
        InvariantFormula = invariantFormula ?? Preset.Invariant;
        VariantFormula = variantFormula ?? LoopPreset.DefaultVariant;
        invariant = new WpParser(InvariantFormula).ParsePostcondition();
        variant = new WpParser(VariantFormula).ParseArithmetic();
        LoopExpressions.Validate(invariant);
        LoopExpressions.Validate(variant);
        body = new WpParser(Preset.Body).ParseProgram();
        PreservationWp = WpEngine.Calculate(Preset.Body, InvariantFormula);
        TerminationWp = WpEngine.Calculate(Preset.Body, $"({VariantFormula}) >= 0 && ({VariantFormula}) < t0");
        InitializationWp = WpEngine.Calculate(Preset.Init, InvariantFormula);
        evaluator.Execute(new WpParser(Preset.Init).ParseProgram(), state);
        preservation = new WpParser(PreservationWp.WeakestPrecondition).ParsePostcondition();
        InitializationHolds = InvariantHolds;
        IsFaulted = !InitializationHolds || !IsNatural(Variant);
    }

    /// <summary>Выполняет один шаг с assert до/после и журналом строгого убывания целого t.</summary>
    public LoopStep Step()
    {
        if (!CanStep) throw new InvalidOperationException("Цикл завершён или проверка контракта остановила выполнение.");
        var before = InvariantHolds;
        Debug.Assert(before, "Inv перед шагом");
        var j = Index;
        var res = Result;
        var t = Variant;
        state["t0"] = t;
        var wpHolds = evaluator.Test(preservation, state);
        // Готовим следующее состояние отдельно: ошибка определённости не оставляет полшага.
        var next = new Dictionary<string, decimal>(state);
        evaluator.Execute(body, next);
        var after = evaluator.Test(invariant, next);
        var nextT = evaluator.Value(variant, next);
        var decreases = IsNatural(t) && IsNatural(nextT) && nextT < t;
        Debug.Assert(after, "Inv после шага");
        Debug.WriteLine($"j={j}: t={t} -> {nextT}; убывает: {decreases}");
        foreach (var pair in next) state[pair.Key] = pair.Value;
        IsFaulted = !before || !after || !wpHolds || !decreases;
        var step = new LoopStep(j, Index, res, Result, before, after, wpHolds, t, nextT, decreases);
        history.Add(step);
        return step;
    }

    private static bool IsNatural(decimal value) => value >= 0 && decimal.Truncate(value) == value;
}
