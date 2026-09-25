using Lab1.Contracts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Lab1
{
    public partial class MainWindow : Window
    {
        private ViewModel viewModel;
        private bool _isInitializing = true;

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
        }

        // ───────────────────────────────────────────────────────────
        // Выбор операции
        // ───────────────────────────────────────────────────────────

        private void OperationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = operationsListBox.SelectedItem as string;

            // Общий сброс
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

        // ───────────────────────────────────────────────────────────
        // Общие обработчики изменений
        // ───────────────────────────────────────────────────────────

        private void TextBox_ParameterChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing) return;

            postIndicator.Fill = Brushes.LightGray;
            resultTextBox.Clear();
            RefreshPreIndicator();
        }

        private void CheckBox_StateChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            postIndicator.Fill = Brushes.LightGray;
            resultTextBox.Clear();
            RefreshPreIndicator();
        }

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

        // ───────────────────────────────────────────────────────────
        // Pre-индикаторы
        // ───────────────────────────────────────────────────────────

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

        // ───────────────────────────────────────────────────────────
        // Выполнить
        // ───────────────────────────────────────────────────────────

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

        // ───────────────────────────────────────────────────────────
        // Показать контракт
        // ───────────────────────────────────────────────────────────

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

        // ───────────────────────────────────────────────────────────
        // Блокировка/разблокировка параметров
        // ───────────────────────────────────────────────────────────

        private void SetRentParametersEnabled(bool enabled)
        {
            chkCarAvailable.IsEnabled = enabled;
            chkUserLoggedIn.IsEnabled = enabled;
            chkActiveRent.IsEnabled = enabled;
        }

        private void SetReturnParametersEnabled(bool enabled)
        {
            chkReturnUserLoggedIn.IsEnabled = enabled;
            chkReturnActiveRent.IsEnabled = enabled;
            txtBalance.IsEnabled = enabled;
            txtTariff.IsEnabled = enabled;
            txtDistance.IsEnabled = enabled;
        }
        private void SetTopUpParametersEnabled(bool enabled)
        {
            chkTopUpUserLoggedIn.IsEnabled = enabled;
            txtTopUpBalance.IsEnabled = enabled;
            txtTopUpValue.IsEnabled = enabled;
        }


        // ───────────────────────────────────────────────────────────
        // Сброс
        // ───────────────────────────────────────────────────────────

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            SetRentParametersEnabled(true);
            SetReturnParametersEnabled(true);
            SetTopUpParametersEnabled(true);

            postIndicator.Fill = Brushes.LightGray;
            resultTextBox.Clear();

            RefreshPreIndicator();
        }
    }
}