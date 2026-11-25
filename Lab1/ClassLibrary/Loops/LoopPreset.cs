namespace ClassLibrary.Loops;

/// <summary>Три обязательных режима анализа массива пробегов завершённых поездок.</summary>
public enum LoopMode { PrefixSum, CountAboveThreshold, PrefixMax }

/// <summary>Единый источник тела цикла, контракта и объяснения режима для GUI и тестов.</summary>
public sealed record LoopPreset(LoopMode Mode, string Title, string Meaning, string Init,
    string Body, string Invariant, string InvariantWords, string Post, string Pre)
{
    public const string Guard = "j < n";
    public const string DefaultVariant = "n - j";

    /// <summary>Возвращает все пресеты в порядке отображения в интерфейсе.</summary>
    public static IReadOnlyList<LoopPreset> All { get; } = Array.AsReadOnly(new[]
    {
        new LoopPreset(LoopMode.PrefixSum, "PrefixSum — общий пробег", "Суммарный пробег обработанных поездок, км.",
            "res := 0; j := 0;", "res := res + item(j); j := j + 1;",
            "res == sum(j) && 0 <= j && j <= n",
            "res равен сумме пробегов первых j поездок; обработано от 0 до n поездок.",
            "j == n && res == sum(n)", "n >= 0"),
        new LoopPreset(LoopMode.CountAboveThreshold, "Count > T — длинные поездки", "Число поездок с пробегом строго больше T км.",
            "res := 0; j := 0;", "if (item(j) > T) { res := res + 1; } else { }; j := j + 1;",
            "res == count(j) && 0 <= j && j <= n",
            "res равен числу поездок с пробегом строго выше T среди первых j; 0 ≤ j ≤ n.",
            "j == n && res == count(n)", "n >= 0"),
        new LoopPreset(LoopMode.PrefixMax, "PrefixMax — рекорд поездки", "Максимальный пробег одной поездки в обработанном префиксе, км.",
            "res := item(0); j := 1;", "if (res < item(j)) { res := item(j); } else { }; j := j + 1;",
            "res == maximum(j) && 1 <= j && j <= n",
            "res равен максимальному пробегу первых j поездок; префикс непустой, 1 ≤ j ≤ n.",
            "j == n && res == maximum(n)", "n >= 1")
    });

    public static LoopPreset For(LoopMode mode) => All.Single(p => p.Mode == mode);

    /// <summary>Формулирует обязательства, не выдавая проверку одного запуска за общее доказательство.</summary>
    public string Obligations(string invariant, string variant, string wp, string terminationWp) =>
        $"Pre: {Pre}; a — неизменный массив целых чисел длины n; T — целое.\n" +
        $"init: {Init}\nB: {Guard}\nS: {Body}\nInv: {invariant}\nPost: {Post}\nt: {variant}\n\n" +
        $"1. Инициализация: Pre ⇒ wp(init, Inv); после init выполнен Inv.\n" +
        $"2. Сохранение: (Inv ∧ B) ⇒ wp(S, Inv)\n   wp = {wp}\n" +
        $"3. Выход: (Inv ∧ ¬B) ⇒ Post\n" +
        $"4. Завершение: (Inv ∧ B) ⇒ t ∈ ℤ ∧ t ≥ 0;\n" +
        $"   (Inv ∧ B ∧ t0 = t) ⇒ wp(S, 0 ≤ t ∧ t < t0)\n   wp = {terminationWp}\n\n" +
        "⇒ — если слева верно, требуется истинность справа; ∧ — и; ¬ — не; ℤ — целые числа.\n" +
        "Def: item(k): 0 ≤ k < n; sum(k), count(k): 0 ≤ k ≤ n; maximum(k): 1 ≤ k ≤ n.\n" +
        "Все индексы целые. Для деления требуется ненулевой знаменатель, для sqrt — аргумент ≥ 0.\n" +
        "GUI проверяет обязательства на состояниях текущего запуска. Общее доказательство пресетов приведено в docs.";
}
