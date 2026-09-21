using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Lab1
{
    public partial class MainWindow : Window
    {
        private ViewModel viewModel;
        private bool _isSyncingFromModel = false;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new ViewModel();

            operationsListBox.Items.Add("Начать аренду");
            operationsListBox.Items.Add("Операция 2");
            operationsListBox.Items.Add("Операция 3");

            parametersGroup.Visibility = Visibility.Collapsed;
            preIndicator.Fill = Brushes.LightGray;
            postIndicator.Fill = Brushes.LightGray;
        }

        private void OperationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = operationsListBox.SelectedItem as string;

            if (string.IsNullOrEmpty(selected))
            {
                parametersHeader.Text = "Ввод параметров";
                parametersGroup.Visibility = Visibility.Collapsed;
                preIndicator.Fill = Brushes.LightGray;
                postIndicator.Fill = Brushes.LightGray;
            }
            else
            {
                parametersHeader.Text = "Ввод параметров: " + selected;

                if (selected == "Начать аренду")
                {
                    parametersGroup.Visibility = Visibility.Visible;
                    UpdatePreIndicator();
                }
                else
                {
                    parametersGroup.Visibility = Visibility.Collapsed;
                    preIndicator.Fill = Brushes.LightGray;
                    postIndicator.Fill = Brushes.LightGray;
                }
            }
        }

        private void CheckBox_StateChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncingFromModel) return;

            postIndicator.Fill = Brushes.LightGray;
            UpdatePreIndicator();
        }

        private void UpdatePreIndicator()
        {
            bool carExists = chkCarExists.IsChecked == true;
            bool carAvailable = chkCarAvailable.IsChecked == true;
            bool userLoggedIn = chkUserLoggedIn.IsChecked == true;
            bool userHasActiveRent = chkActiveRent.IsChecked == true;

            viewModel.SetParameters(carExists, carAvailable, userLoggedIn, userHasActiveRent);

            preIndicator.Fill = viewModel.GetCarRentPre()
                ? Brushes.Green
                : Brushes.Red;
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = operationsListBox.SelectedItem as string;

            if (selected != "Начать аренду")
            {
                MessageBox.Show("Выберите операцию «Начать аренду».",
                                "Информация",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

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

            // Считываем Post из модели сразу после операции,
            bool post = viewModel.GetCarRentPost();

            // Синхронизируем интерфейс
            _isSyncingFromModel = true;
            chkCarAvailable.IsChecked = viewModel.CarAvailable;
            chkActiveRent.IsChecked = viewModel.UserHasActiveRent;
            _isSyncingFromModel = false;

            // Обновляем индикатор Post
            postIndicator.Fill = post ? Brushes.Green : Brushes.Red;
        }

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
    }
}