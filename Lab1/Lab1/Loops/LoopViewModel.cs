using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace Lab1.Loops;

/// <summary>Строка таблицы. Изменения значений сбрасывают запущенный расчёт через ViewModel.</summary>
public sealed class TripRow(int index, int value) : INotifyPropertyChanged
{
    private int distance = value;
    private bool current, processed, changed;
    public int Index { get; } = index;
    public int Distance { get => distance; set { distance = value; Notify(); } }
    public bool IsCurrent { get => current; set { current = value; Notify(); } }
    public bool IsProcessed { get => processed; set { processed = value; Notify(); } }
    public bool IsChanged { get => changed; set { changed = value; Notify(); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

/// <summary>Команда WPF с явным обновлением доступности при смене состояния цикла.</summary>
public sealed class LoopCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => execute();
    public event EventHandler? CanExecuteChanged;
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>Описания элементов интерфейса до подключения бэкенда.</summary>
public sealed record LoopPreset(string Title, string Invariant, string InvariantWords, string Meaning)
{
    public static IReadOnlyList<LoopPreset> All { get; } = new[]
    {
        new LoopPreset("PrefixSum — общий пробег", "res == sum(j) && 0 <= j && j <= n",
            "res равен сумме пробегов первых j поездок; 0 ≤ j ≤ n.", "Суммарный пробег поездок, км."),
        new LoopPreset("Count > T — длинные поездки", "res == count(j) && 0 <= j && j <= n",
            "res равен числу пробегов строго выше T среди первых j; 0 ≤ j ≤ n.", "Число длинных поездок."),
        new LoopPreset("PrefixMax — рекорд поездки", "res == maximum(j) && 1 <= j && j <= n",
            "res равен максимальному пробегу первых j поездок; 1 ≤ j ≤ n.", "Максимальный пробег поездки, км.")
    };
}

/// <summary>
/// Первый этап: самостоятельный фронтенд. Доступны ввод, пресеты, генератор и таблица.
/// Вычисление wp и исполнение цикла подключаются следующим коммитом бэкенда.
/// </summary>
public sealed class LoopViewModel : INotifyPropertyChanged, IDisposable
{
    private LoopPreset selectedPreset = LoopPreset.All[0];
    private string length = "6", minimum = "5", maximum = "80", threshold = "30";
    private string invariantFormula = "", invariantWords = "", variantFormula = "n - j";
    private string status = "";
    private bool invalidData;
    public ObservableCollection<TripRow> Trips { get; } = new();
    public IReadOnlyList<LoopPreset> Presets => LoopPreset.All;
    public LoopCommand GenerateCommand { get; }
    public LoopCommand ResetCommand { get; }
    public LoopCommand StepCommand { get; } = new(() => { }, () => false);
    public LoopCommand RunCommand { get; } = new(() => { }, () => false);
    public LoopCommand PauseCommand { get; } = new(() => { }, () => false);
    public LoopCommand DemoCommand { get; }
    public bool IsRunning => false;
    public bool CanEdit => true;
    public string Length { get => length; set { length = value; Changed(); } }
    public string Minimum { get => minimum; set { minimum = value; Changed(); } }
    public string Maximum { get => maximum; set { maximum = value; Changed(); } }
    public string Threshold { get => threshold; set { threshold = value; Changed(); } }
    public string InvariantFormula { get => invariantFormula; set { invariantFormula = value; Changed(); } }
    public string InvariantWords { get => invariantWords; set { invariantWords = value; Notify(); } }
    public string VariantFormula { get => variantFormula; set { variantFormula = value; Changed(); } }
    public LoopPreset SelectedPreset
    {
        get => selectedPreset;
        set { selectedPreset = value; invariantFormula = value.Invariant;
            invariantWords = value.InvariantWords; variantFormula = "n - j"; Reset(); }
    }
    public string Status => status;
    public string WpText => "Движок wp будет подключён на этапе бэкенда.";
    public string ProofText => "Здесь будут четыре условия корректности выбранного цикла.";
    public string Trace => "Журнал появится после подключения исполнения цикла.";
    public bool? InvariantBefore => null;
    public bool? InvariantAfter => null;
    public bool? Post => null;
    public string StateText => "j = —    res = —    t = —";
    public string StateExplanation => selectedPreset.Meaning;
    public double VariantValue => 0;
    public double VariantMaximum => 1;
    public event PropertyChangedEventHandler? PropertyChanged;

    public LoopViewModel()
    {
        GenerateCommand = new(Generate);
        ResetCommand = new(Reset, () => !invalidData);
        DemoCommand = new(LoadDemo);
        invariantFormula = selectedPreset.Invariant;
        invariantWords = selectedPreset.InvariantWords;
        LoadDemo();
    }

    /// <summary>Показывает общие демонстрационные данные всех режимов.</summary>
    public void LoadDemo()
    {
        ReplaceTrips(new[] { 12, 48, 30, 65, 8, 42 });
        length = "6"; threshold = "30"; Reset();
    }

    private void Generate()
    {
        try
        {
            var n = ReadInt(length, "Длина");
            var min = ReadInt(minimum, "Минимум"); var max = ReadInt(maximum, "Максимум");
            if (n < 0 || n > 1000) throw new ArgumentException("Длина должна быть от 0 до 1000.");
            if (min > max) throw new ArgumentException("Минимум не должен превышать максимум.");
            ReplaceTrips(Enumerable.Range(0, n).Select(_ => (int)Random.Shared.NextInt64(min, (long)max + 1)));
            Reset();
        }
        catch (ArgumentException ex) { status = "Ошибка: " + ex.Message; Notify(string.Empty); }
    }

    /// <summary>Фиксирует ввод без проверки алгоритма, которого ещё нет в первом срезе.</summary>
    public void Reset()
    {
        try { ReadInt(threshold, "Порог T");
            status = "Интерфейс готов. Исполнение и проверки подключаются на этапе бэкенда."; }
        catch (ArgumentException ex) { status = "Ошибка: " + ex.Message; }
        Notify(string.Empty);
    }

    private void ReplaceTrips(IEnumerable<int> data)
    {
        foreach (var row in Trips) row.PropertyChanged -= RowChanged;
        Trips.Clear(); invalidData = false;
        foreach (var value in data)
        {
            var row = new TripRow(Trips.Count, value); row.PropertyChanged += RowChanged; Trips.Add(row);
        }
        ResetCommand.Refresh();
    }
    private void RowChanged(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName == nameof(TripRow.Distance)) Changed(); }
    public void SetDataValidationError(bool invalid)
    {
        invalidData = invalid;
        status = invalid ? "Исправьте пробег в таблице или нажмите Esc." : "Данные исправлены.";
        ResetCommand.Refresh(); Notify(string.Empty);
    }
    private void Changed()
    { status = "Данные изменены. Выполнение будет доступно после подключения бэкенда."; Notify(string.Empty); }
    private static int ReadInt(string text, string name) => int.TryParse(text, NumberStyles.Integer,
        CultureInfo.InvariantCulture, out var value) ? value : throw new ArgumentException($"{name}: введите целое число.");
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    public void Pause() { }
    public void Step() { }
    public void Dispose() { }
}
