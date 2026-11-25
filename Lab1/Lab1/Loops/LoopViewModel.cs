using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using ClassLibrary.Loops;

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

/// <summary>
/// MVVM-модель вкладки ЛР3. Таймер выполняет ровно один шаг за тик и оставляет GUI
/// отзывчивым. Все вычисления и проверка формул делегированы ClassLibrary.
/// </summary>
public sealed class LoopViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(250) };
    private LoopSession? session;
    private LoopPreset selectedPreset = LoopPreset.All[0];
    private string length = "6", minimum = "5", maximum = "80", threshold = "30";
    private string invariantFormula = "", invariantWords = "", variantFormula = LoopPreset.DefaultVariant;
    private string status = "", wpText = "", proofText = "", trace = "";
    private bool? before, after;
    private bool hasInvalidData;

    public ObservableCollection<TripRow> Trips { get; } = new();
    public IReadOnlyList<LoopPreset> Presets => LoopPreset.All;
    public LoopCommand GenerateCommand { get; }
    public LoopCommand ResetCommand { get; }
    public LoopCommand StepCommand { get; }
    public LoopCommand RunCommand { get; }
    public LoopCommand PauseCommand { get; }
    public LoopCommand DemoCommand { get; }
    public bool IsRunning => timer.IsEnabled;
    public bool CanEdit => !IsRunning;
    public string Length { get => length; set { length = value; Invalidate(); } }
    public string Minimum { get => minimum; set { minimum = value; Invalidate(); } }
    public string Maximum { get => maximum; set { maximum = value; Invalidate(); } }
    public string Threshold { get => threshold; set { threshold = value; Invalidate(); } }
    public string InvariantFormula { get => invariantFormula; set { invariantFormula = value; Invalidate(); } }
    public string InvariantWords { get => invariantWords; set { invariantWords = value; Notify(); } }
    public string VariantFormula { get => variantFormula; set { variantFormula = value; Invalidate(); } }
    public LoopPreset SelectedPreset
    {
        get => selectedPreset;
        set
        {
            selectedPreset = value;
            invariantFormula = value.Invariant;
            invariantWords = value.InvariantWords;
            variantFormula = LoopPreset.DefaultVariant;
            Invalidate();
            Reset();
        }
    }
    public string Status => status;
    public string WpText => wpText;
    public string ProofText => proofText;
    public string Trace => trace;
    public bool? InvariantBefore => before;
    public bool? InvariantAfter => after;
    public bool? Post => session is { IsComplete: true } ? session.PostHolds : null;
    public string StateText => session is null ? "j = —    res = —    t = —" :
        $"j = {session.Index} / {session.Data.Count}    res = {session.Result}    t = {session.Variant}";
    public double VariantValue => session is null ? 0 : (double)session.Variant;
    public double VariantMaximum { get; private set; } = 1;
    public string StateExplanation => session is null ? "Инициализируйте цикл после изменения данных." :
        $"Обработано {session.Index} поездок. {selectedPreset.Meaning} Осталось {session.Data.Count - session.Index}.";

    public event PropertyChangedEventHandler? PropertyChanged;

    public LoopViewModel()
    {
        GenerateCommand = new(Generate, () => CanEdit);
        ResetCommand = new(Reset, () => CanEdit && !hasInvalidData);
        StepCommand = new(Step, () => CanEdit && !hasInvalidData && session?.CanStep == true);
        RunCommand = new(() => { timer.Start(); status = "Выполняется по одному шагу каждые 250 мс."; Refresh(); },
            () => CanEdit && !hasInvalidData && session?.CanStep == true);
        PauseCommand = new(Pause, () => IsRunning);
        DemoCommand = new(LoadDemo, () => CanEdit);
        timer.Tick += OnTick;
        invariantFormula = selectedPreset.Invariant;
        invariantWords = selectedPreset.InvariantWords;
        LoadDemo();
    }

    /// <summary>Загружает одинаковые данные для сравнения всех трёх алгоритмов.</summary>
    public void LoadDemo()
    {
        ReplaceTrips(new[] { 12, 48, 30, 65, 8, 42 });
        length = "6";
        threshold = "30";
        Reset();
    }

    /// <summary>Генерирует целочисленные пробеги с включёнными обеими границами диапазона.</summary>
    private void Generate() => Safely(() =>
    {
        var n = ReadInt(length, "Длина");
        var min = ReadInt(minimum, "Минимум");
        var max = ReadInt(maximum, "Максимум");
        if (n < 0 || n > 1000) throw new ArgumentException("Длина должна быть от 0 до 1000.");
        if (min > max) throw new ArgumentException("Минимум не должен превышать максимум.");
        ReplaceTrips(Enumerable.Range(0, n).Select(_ => (int)Random.Shared.NextInt64(min, (long)max + 1)).ToArray());
        Reset();
    });

    /// <summary>Создаёт новый снимок массива и выводит фактически рассчитанные wp.</summary>
    public void Reset() => Safely(() =>
    {
        if (hasInvalidData) throw new ArgumentException("Исправьте неверное значение в таблице или нажмите Esc.");
        timer.Stop();
        session = null;
        before = after = null;
        trace = wpText = proofText = "";
        session = new LoopSession(selectedPreset.Mode, Trips.Select(t => t.Distance), ReadInt(threshold, "Порог T"),
            invariantFormula, variantFormula);
        before = after = session.InitializationHolds;
        VariantMaximum = Math.Max(1, (double)session.Variant);
        wpText = $"(Inv ∧ j < n) ⇒ ({session.PreservationWp.WeakestPrecondition})\n\n" +
            session.PreservationWp.WeakestPreconditionHumanReadable + "\n\n" + session.PreservationWp.Trace;
        proofText = session.Obligations;
        status = session.IsFaulted ? "Инициализация нарушает Inv либо t не является неотрицательным целым. Исправьте формулу." :
            session.IsComplete ? "Цикл завершён при инициализации. Проверьте Post." : "Инициализация выполнена. Можно сделать шаг или запустить цикл.";
        trace = $"init: j={session.Index}, res={session.Result}, t={session.Variant}; Inv={session.InitializationHolds}\n";
        UpdateRows(-1);
        Refresh();
    });

    /// <summary>Добавляет наблюдения шага и останавливается при нарушении любого обязательства.</summary>
    public void Step() => Safely(() =>
    {
        if (session?.CanStep != true) return;
        var step = session.Step();
        before = step.InvariantBefore;
        after = step.InvariantAfter;
        trace += $"j: {step.IndexBefore} → {step.IndexAfter}; res: {step.ResultBefore} → {step.ResultAfter}; " +
            $"t: {step.VariantBefore} → {step.VariantAfter}; убывает={step.VariantDecreases}; " +
            $"Inv до={before}, после={after}; wp={step.PreservationHolds}\n";
        UpdateRows(step.IndexBefore);
        if (session.IsComplete || session.IsFaulted) timer.Stop();
        status = session.IsFaulted ? "Остановка: нарушен Inv, сохранение wp или строгое убывание целого t. См. журнал." :
            session.IsComplete ? $"Завершено. Post = {session.PostHolds}. Результат: {session.Result}." : "Шаг выполнен.";
        Refresh();
    });

    public void Pause() { timer.Stop(); status = "Пауза. Можно продолжить с текущего шага."; Refresh(); }
    private void OnTick(object? sender, EventArgs e) => Step();

    /// <summary>Изменение входа не оставляет в интерфейсе результатов от старого массива или формулы.</summary>
    private void Invalidate()
    {
        timer.Stop();
        session = null;
        before = after = null;
        wpText = proofText = trace = "";
        status = "Данные изменены. Нажмите «Инициализировать».";
        UpdateRows(-1);
        Refresh();
    }

    private void ReplaceTrips(IEnumerable<int> values)
    {
        hasInvalidData = false;
        foreach (var row in Trips) row.PropertyChanged -= RowChanged;
        Trips.Clear();
        foreach (var value in values)
        {
            var row = new TripRow(Trips.Count, value);
            row.PropertyChanged += RowChanged;
            Trips.Add(row);
        }
    }

    private void RowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TripRow.Distance)) Invalidate();
    }

    /// <summary>Не допускает расчёт по старому значению ячейки, пока в редакторе находится неверный ввод.</summary>
    public void SetDataValidationError(bool invalid)
    {
        hasInvalidData = invalid;
        Invalidate();
        if (invalid) status = "Пробег должен быть целым числом. Исправьте ячейку или нажмите Esc.";
        Refresh();
    }

    private void UpdateRows(int lastRead)
    {
        foreach (var row in Trips)
        {
            row.IsCurrent = session?.CanStep == true && row.Index == session.Index;
            row.IsProcessed = session is not null && row.Index < session.Index;
            row.IsChanged = row.Index == lastRead;
        }
    }

    private void Safely(Action action)
    {
        try { action(); }
        catch (Exception ex) when (ex is ArgumentException or FormatException or ArithmeticException or InvalidOperationException)
        {
            timer.Stop();
            session = null;
            before = after = null;
            wpText = proofText = "";
            status = "Ошибка: " + ex.Message;
            UpdateRows(-1);
            Refresh();
        }
    }

    private static int ReadInt(string text, string name) => int.TryParse(text, NumberStyles.Integer,
        CultureInfo.InvariantCulture, out var value) ? value : throw new ArgumentException($"{name}: введите целое число.");
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    private void Refresh()
    {
        Notify(string.Empty);
        GenerateCommand.Refresh(); ResetCommand.Refresh(); StepCommand.Refresh();
        RunCommand.Refresh(); PauseCommand.Refresh(); DemoCommand.Refresh();
    }

    /// <summary>Отсоединяет таймер при закрытии окна или уходе с вкладки.</summary>
    public void Dispose() { timer.Stop(); timer.Tick -= OnTick; }
}
