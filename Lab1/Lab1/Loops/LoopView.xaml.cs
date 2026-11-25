using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Lab1.Loops;

/// <summary>Только связка представления с MVVM и фиксация редактирования таблицы.</summary>
public partial class LoopView : UserControl
{
    private int validationErrors;
    public LoopViewModel Model { get; } = new();
    public LoopView()
    {
        InitializeComponent();
        DataContext = Model;
        // Переключение вкладки ставит автоматическое исполнение на паузу.
        Unloaded += (_, _) => { if (Model.IsRunning) Model.Pause(); };
    }

    private void TripsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        // Отложенная фиксация привязки завершится до следующего нажатия команды.
        if (e.EditAction == DataGridEditAction.Commit && e.EditingElement is TextBox editor)
            editor.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
    }

    private void TripsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) TripsGrid.CommitEdit(DataGridEditingUnit.Row, true);
    }

    private void TripsGrid_ValidationError(object sender, ValidationErrorEventArgs e)
    {
        validationErrors = Math.Max(0, validationErrors + (e.Action == ValidationErrorEventAction.Added ? 1 : -1));
        Model.SetDataValidationError(validationErrors > 0);
        if (e.Action == ValidationErrorEventAction.Added)
            TripsGrid.ToolTip = "Пробег должен быть целым числом от -2147483648 до 2147483647.";
        else TripsGrid.ToolTip = null;
    }
}
