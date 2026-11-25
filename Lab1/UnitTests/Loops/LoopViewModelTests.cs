using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Controls;
using ClassLibrary.Loops;
using Lab1;
using Lab1.Loops;
using Xunit;

namespace LoopTests;

public class LoopViewModelTests
{
    /// <summary>WPF и DispatcherTimer требуют STA; исключения передаются обратно в xUnit.</summary>
    private static void Sta(Action action)
    {
        Exception? error = null;
        var thread = new Thread(() => { try { action(); } catch (Exception ex) { error = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "STA-проверка не завершилась за 20 секунд.");
        if (error is not null) ExceptionDispatchInfo.Capture(error).Throw();
    }

    [Fact]
    public void DefaultDemo_RunsAndInvalidatesOnEdit() => Sta(() =>
    {
        using var model = new LoopViewModel();
        Assert.True(model.StepCommand.CanExecute(null));
        while (model.StepCommand.CanExecute(null)) model.Step();
        Assert.Equal(true, model.Post);
        Assert.Contains("205", model.StateText);
        model.Trips[0].Distance = 99;
        Assert.Null(model.Post);
        Assert.Empty(model.WpText);
        Assert.False(model.StepCommand.CanExecute(null));
        model.Reset();
        Assert.True(model.StepCommand.CanExecute(null));
    });

    [Fact]
    public void FormulaAndThresholdChanges_InvalidateResults() => Sta(() =>
    {
        using var model = new LoopViewModel();
        model.Threshold = "48";
        Assert.Empty(model.WpText);
        model.SelectedPreset = LoopPreset.For(LoopMode.CountAboveThreshold);
        while (model.StepCommand.CanExecute(null)) model.Step();
        Assert.Contains("res = 1", model.StateText);
        model.InvariantFormula = "res == 999";
        model.Reset();
        Assert.Equal(false, model.InvariantBefore);
        Assert.False(model.StepCommand.CanExecute(null));
    });

    [Fact]
    public void InvalidInputs_AreShownWithoutThrowing() => Sta(() =>
    {
        using var model = new LoopViewModel();
        model.Threshold = "abc";
        model.Reset();
        Assert.StartsWith("Ошибка:", model.Status);
        model.Threshold = "30";
        model.VariantFormula = "1 / 0";
        model.Reset();
        Assert.StartsWith("Ошибка:", model.Status);
        model.Length = "0";
        model.VariantFormula = "n-j";
        model.GenerateCommand.Execute(null);
        Assert.Equal(true, model.Post);
        model.SelectedPreset = LoopPreset.For(LoopMode.PrefixMax);
        Assert.Contains("хотя бы одна", model.Status);
    });

    [Fact]
    public void RunAndPause_ToggleCommandsAndEditing() => Sta(() =>
    {
        using var model = new LoopViewModel();
        model.RunCommand.Execute(null);
        Assert.True(model.IsRunning);
        Assert.False(model.CanEdit);
        Assert.False(model.StepCommand.CanExecute(null));
        model.PauseCommand.Execute(null);
        Assert.False(model.IsRunning);
        Assert.True(model.StepCommand.CanExecute(null));
    });

    [Fact]
    public void InvalidTableValue_BlocksExecutionUntilCorrected() => Sta(() =>
    {
        using var model = new LoopViewModel();
        model.SetDataValidationError(true);
        Assert.False(model.ResetCommand.CanExecute(null));
        Assert.False(model.RunCommand.CanExecute(null));
        Assert.False(model.StepCommand.CanExecute(null));
        model.SetDataValidationError(false);
        model.Reset();
        Assert.True(model.StepCommand.CanExecute(null));
    });

    [Fact]
    public void AutomaticRun_ReachesPostWithoutBlockingDispatcher() => Sta(() =>
    {
        using var model = new LoopViewModel();
        var frame = new System.Windows.Threading.DispatcherFrame();
        var watchdog = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        var started = DateTime.UtcNow;
        watchdog.Tick += (_, _) =>
        {
            if (!model.IsRunning || DateTime.UtcNow - started > TimeSpan.FromSeconds(5)) frame.Continue = false;
        };
        model.RunCommand.Execute(null);
        watchdog.Start();
        System.Windows.Threading.Dispatcher.PushFrame(frame);
        watchdog.Stop();
        Assert.False(model.IsRunning);
        Assert.Equal(true, model.Post);
    });

    [Fact]
    public void Window_ContainsOriginalTabsAndNewLoopView() => Sta(() =>
    {
        var window = new MainWindow();
        try
        {
            var tabs = Assert.IsType<TabControl>(window.Content);
            Assert.Equal(3, tabs.Items.Count);
            var last = Assert.IsType<TabItem>(tabs.Items[2]);
            var view = Assert.IsType<LoopView>(last.Content);
            Assert.IsType<LoopViewModel>(view.DataContext);
            // Привязки активируются при реальной отрисовке, а не только при конструировании окна.
            tabs.SelectedIndex = 2;
            window.Show();
            window.UpdateLayout();
            view.Model.Dispose();
        }
        finally { window.Close(); }
    });
}
