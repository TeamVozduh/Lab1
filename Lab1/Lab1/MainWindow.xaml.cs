using Lab1.Contracts;
using ClassLibrary.Wp;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Lab1
{
    /// <summary>Hosts the car-sharing contract demo and the wp calculator.</summary>
    public partial class MainWindow : Window
    {
        private ViewModel viewModel;
        // XAML change events can fire before the constructor finishes wiring the view.
        private bool _isInitializing = true;
        // Keep the last successful calculation for the triad button.
        private WpResult? wpResult;

        /// <summary>Initializes both tabs and connects live wp input validation.</summary>
        public MainWindow()
        {
            InitializeComponent();
            viewModel = new ViewModel();

            operationsListBox.Items.Add(ContractRegistry.RentCarKey);
            operationsListBox.Items.Add(ContractRegistry.ReturnCarKey);
            operationsListBox.Items.Add(ContractRegistry.TopUpKey);

            rentParametersGroup.Visibility = Visibility.Collapsed;
            returnParametersGroup.Visibility = Visibility.Collapsed;
            topUpParametersGroup.Visibility = Visibility.Collapsed;

            preIndicator.Fill = Brushes.LightGray;
            postIndicator.Fill = Brushes.LightGray;

            _isInitializing = false;

            // Changes to either WP input invalidate the displayed calculation.
            txtPostCondition.TextChanged += WpInputChanged;
            txtFragmentCode.TextChanged += WpInputChanged;
            RefreshPostDescription();
        }

        /// <summary>Resets the previous result and shows inputs for the selected operation.</summary>
        private void OperationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = operationsListBox.SelectedItem as string;

            // A selection change invalidates the previous operation's result.
            resultTextBox.Clear();
            postIndicator.Fill = Brushes.LightGray;
            preIndicator.Fill = Brushes.LightGray;
            rentParametersGroup.Visibility = Visibility.Collapsed;
            returnParametersGroup.Visibility = Visibility.Collapsed;
            topUpParametersGroup.Visibility = Visibility.Collapsed;

            if (string.IsNullOrEmpty(selected))
            {
                parametersHeader.Text = "Ввод параметров";
                return;
            }

            parametersHeader.Text = "Ввод параметров: " + selected;

            if (selected == ContractRegistry.RentCarKey)
            {
                rentParametersGroup.Visibility = Visibility.Visible;
                UpdateRentPreIndicator();
            }
            else if (selected == ContractRegistry.ReturnCarKey)
            {
                returnParametersGroup.Visibility = Visibility.Visible;
                UpdateReturnPreIndicator();
            }
            else if (selected == ContractRegistry.TopUpKey)
            {
                topUpParametersGroup.Visibility = Visibility.Visible;
                UpdateTopUpPreIndicator();
            }
        }

        /// <summary>Invalidates the previous Post result and rechecks numeric inputs.</summary>
        private void TextBox_ParameterChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing) return;

            postIndicator.Fill = Brushes.LightGray;
            resultTextBox.Clear();
            RefreshPreIndicator();
        }

        /// <summary>Invalidates the previous Post result and rechecks checkbox inputs.</summary>
        private void CheckBox_StateChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            postIndicator.Fill = Brushes.LightGray;
            resultTextBox.Clear();
            RefreshPreIndicator();
        }

        /// <summary>Updates the Pre indicator for the selected operation.</summary>
        private void RefreshPreIndicator()
        {
            var selected = operationsListBox.SelectedItem as string;

            if (selected == ContractRegistry.RentCarKey)
                UpdateRentPreIndicator();
            else if (selected == ContractRegistry.ReturnCarKey)
                UpdateReturnPreIndicator();
            else if (selected == ContractRegistry.TopUpKey)
                UpdateTopUpPreIndicator();
        }

        /// <summary>Reads top-up inputs and displays whether their precondition holds.</summary>
        private void UpdateTopUpPreIndicator()
        {
            bool userLoggedIn = chkTopUpUserLoggedIn.IsChecked == true;

            decimal.TryParse(txtTopUpBalance.Text, out decimal balance);
            decimal.TryParse(txtTopUpValue.Text, out decimal topUpValue);

            viewModel.SetBalanceTopUpParameters(userLoggedIn, balance, topUpValue);

            preIndicator.Fill = viewModel.GetBalanceTopUpPre()
                ? Brushes.Green
                : Brushes.Red;
        }

        /// <summary>Reads rental inputs and displays whether their precondition holds.</summary>
        private void UpdateRentPreIndicator()
        {
            bool carAvailable = chkCarAvailable.IsChecked == true;
            bool userLoggedIn = chkUserLoggedIn.IsChecked == true;
            bool userHasActiveRent = chkActiveRent.IsChecked == true;

            viewModel.SetCarRentParameters(carAvailable, userLoggedIn, userHasActiveRent);

            preIndicator.Fill = viewModel.GetCarRentPre()
                ? Brushes.Green
                : Brushes.Red;
        }

        /// <summary>Reads return inputs and displays whether their precondition holds.</summary>
        private void UpdateReturnPreIndicator()
        {
            bool userLoggedIn = chkReturnUserLoggedIn.IsChecked == true;
            bool userHasActiveRent = chkReturnActiveRent.IsChecked == true;

            decimal.TryParse(txtBalance.Text, out decimal balance);
            decimal.TryParse(txtTariff.Text, out decimal tariff);
            int.TryParse(txtDistance.Text, out int distance);

            viewModel.SetCarReturnParameters(userLoggedIn, userHasActiveRent,
                                             balance, tariff, distance);

            preIndicator.Fill = viewModel.GetCarReturnPre()
                ? Brushes.Green
                : Brushes.Red;
        }

        /// <summary>Routes the Execute button to the selected car-sharing operation.</summary>
        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = operationsListBox.SelectedItem as string;

            if (selected == ContractRegistry.RentCarKey)
                ExecuteRent();
            else if (selected == ContractRegistry.ReturnCarKey)
                ExecuteReturn();
            else if (selected == ContractRegistry.TopUpKey)
                ExecuteTopUp();
            else
                MessageBox.Show("Выберите операцию.",
                                "Информация",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
        }

        /// <summary>Runs a valid rental and reports its Post result to the user.</summary>
        private void ExecuteRent()
        {
            if (!viewModel.GetCarRentPre())
            {
                MessageBox.Show("Предусловие не выполнено — операция не может быть выполнена.",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            try
            {
                viewModel.ExecuteCarRent();
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Нарушение предусловия",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool post = viewModel.GetCarRentPost();
            postIndicator.Fill = post ? Brushes.Green : Brushes.Red;

            if (post)
                resultTextBox.Text = "Автомобиль становится недоступным; " +
                                     "у пользователя появляется активная аренда.";
            else
                resultTextBox.Text = "Постусловие не выполнено.";

            SetRentParametersEnabled(false);
        }

        /// <summary>Closes a valid rental and reports its new balance and Post result.</summary>
        private void ExecuteReturn()
        {
            if (!viewModel.GetCarReturnPre())
            {
                MessageBox.Show("Предусловие не выполнено — операция не может быть выполнена.",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            try
            {
                viewModel.ExecuteCarReturn();
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Нарушение предусловия",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }

            bool post = viewModel.GetCarReturnPost();
            postIndicator.Fill = post ? Brushes.Green : Brushes.Red;

            if (post)
            {
                decimal newBalance = viewModel.UserBalance;

                resultTextBox.Text =
                    "Аренда закрывается; баланс уменьшается на произведение расстояния и тарифа." +
                    "\nНовый баланс: " + newBalance.ToString("0.##");
            }
            else
            {
                resultTextBox.Text = "Постусловие не выполнено.";
            }

            SetReturnParametersEnabled(false);
        }

        /// <summary>Runs a valid top-up and reports its new balance and Post result.</summary>
        private void ExecuteTopUp()
        {
            if (!viewModel.GetBalanceTopUpPre())
            {
                MessageBox.Show("Предусловие не выполнено — операция не может быть выполнена.",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            try
            {
                viewModel.ExecuteBalanceTopUp();
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Нарушение предусловия",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool post = viewModel.GetBalanceTopUpPost();
            postIndicator.Fill = post ? Brushes.Green : Brushes.Red;

            if (post)
            {
                decimal newBalance = viewModel.TopUpBalance;

                resultTextBox.Text =
                    "Баланс пользователя увеличивается на указанную сумму." +
                    "\nНовый баланс: " + newBalance.ToString("0.##");
            }
            else
            {
                resultTextBox.Text = "Постусловие не выполнено.";
            }

            SetTopUpParametersEnabled(false);
        }

        /// <summary>Opens the selected operation's contract in a modal dialog.</summary>
        private void ContractShowButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = operationsListBox.SelectedItem as string;

            if (string.IsNullOrEmpty(selected))
            {
                MessageBox.Show("Сначала выберите операцию.",
                                "Информация",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            var contractWindow = new ContractWindow(selected) { Owner = this };
            contractWindow.ShowDialog();
        }

        /// <summary>Controls whether rental parameters can be edited after execution.</summary>
        private void SetRentParametersEnabled(bool enabled)
        {
            chkCarAvailable.IsEnabled = enabled;
            chkUserLoggedIn.IsEnabled = enabled;
            chkActiveRent.IsEnabled = enabled;
        }

        /// <summary>Controls whether return parameters can be edited after execution.</summary>
        private void SetReturnParametersEnabled(bool enabled)
        {
            chkReturnUserLoggedIn.IsEnabled = enabled;
            chkReturnActiveRent.IsEnabled = enabled;
            txtBalance.IsEnabled = enabled;
            txtTariff.IsEnabled = enabled;
            txtDistance.IsEnabled = enabled;
        }

        /// <summary>Controls whether top-up parameters can be edited after execution.</summary>
        private void SetTopUpParametersEnabled(bool enabled)
        {
            chkTopUpUserLoggedIn.IsEnabled = enabled;
            txtTopUpBalance.IsEnabled = enabled;
            txtTopUpValue.IsEnabled = enabled;
        }


        /// <summary>Unlocks operation inputs and clears the displayed execution result.</summary>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SetRentParametersEnabled(true);
            SetReturnParametersEnabled(true);
            SetTopUpParametersEnabled(true);

            postIndicator.Fill = Brushes.LightGray;
            resultTextBox.Clear();

            RefreshPreIndicator();
        }

        /// <summary>Clears stale wp output whenever the fragment or Post changes.</summary>
        private void WpInputChanged(object sender, TextChangedEventArgs e)
        {
            wpResult = null;
            txtTrace.Clear();
            txtWpShort.Clear();
            txtWpHumanReadable.Clear();
            txtTriad.Clear();
            btnShowTriad.IsEnabled = false;
            RefreshPostDescription();
        }

        /// <summary>Shows a live Post explanation or a prompt for incomplete input.</summary>
        private void RefreshPostDescription()
        {
            try
            {
                txtPostHumanReadable.Text = WpEngine.DescribePostcondition(txtPostCondition.Text);
            }
            catch (WpParseException)
            {
                txtPostHumanReadable.Text = "Введите корректное постусловие";
            }
        }

        /// <summary>Calculates wp and fills the result panes, or shows a parse error.</summary>
        private void CalculateWpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                wpResult = WpEngine.Calculate(txtFragmentCode.Text, txtPostCondition.Text);
                txtPostHumanReadable.Text = wpResult.PostHumanReadable;
                txtTrace.Text = wpResult.Trace;
                txtWpShort.Text = wpResult.WeakestPrecondition;
                txtWpHumanReadable.Text = wpResult.WeakestPreconditionHumanReadable;
                txtTriad.Clear();
                btnShowTriad.IsEnabled = true;
            }
            catch (WpParseException ex)
            {
                wpResult = null;
                txtTrace.Text = "Ошибка ввода: " + ex.Message;
                txtWpShort.Clear();
                txtWpHumanReadable.Clear();
                txtTriad.Clear();
                btnShowTriad.IsEnabled = false;
            }
        }

        /// <summary>Shows the Hoare triple from the last successful calculation.</summary>
        private void ShowTriadButton_Click(object sender, RoutedEventArgs e)
        {
            if (wpResult is not null)
                txtTriad.Text = wpResult.Triad;
        }

        /// <summary>Loads the two-value maximum example into the editable wp inputs.</summary>
        private void PresetMaxButton_Click(object sender, RoutedEventArgs e)
        {
            txtPostCondition.Text = "max > 100";
            txtFragmentCode.Text = "if (x1 >= x2) { max := x1; } else { max := x2; }";
        }

        /// <summary>Loads the quadratic example with real-root and fallback branches.</summary>
        private void PresetQuadraticButton_Click(object sender, RoutedEventArgs e)
        {
            txtPostCondition.Text = "(D >= 0 && a*x*x + b*x + c == 0) || (D < 0 && x == -999)";
            txtFragmentCode.Text = "D := b*b - 4*a*c; if (D >= 0) { x := (-b + sqrt(D)) / (2*a); } else { x := -999; }";
        }

        /// <summary>Loads an example whose wp depends on assignment order.</summary>
        private void PresetSequenceButton_Click(object sender, RoutedEventArgs e)
        {
            txtPostCondition.Text = "y = x - 9 && x > 15";
            txtFragmentCode.Text = "y := x + 1; x := x + 10;";
        }

        /// <summary>Leaves splitter resizing to WPF; no extra view state is required.</summary>
        private void GridSplitter_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {

        }
    }
}
