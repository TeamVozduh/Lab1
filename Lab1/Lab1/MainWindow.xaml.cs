using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Lab1
{
    public partial class MainWindow : Window
    {
        private ViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new ViewModel();

            operationsListBox.Items.Add("Начать аренду");
            operationsListBox.Items.Add("Операция 2");
            operationsListBox.Items.Add("Операция 3");

            parametersGroup.Visibility = Visibility.Collapsed;
            preIndicator.Fill = Brushes.LightGray;
        }

        private void OperationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = operationsListBox.SelectedItem as string;

            if (string.IsNullOrEmpty(selected))
            {
                parametersHeader.Text = "Ввод параметров";
                parametersGroup.Visibility = Visibility.Collapsed;
                preIndicator.Fill = Brushes.LightGray;
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
            postIndicator.Fill = Brushes.LightGray;
            UpdatePreIndicator();
        }

        private void UpdatePreIndicator()
        {
            bool carExists = chkCarExists.IsChecked == true;
            bool carAvailable = chkCarAvailable.IsChecked == true;
            bool userLoggedIn = chkUserLoggedIn.IsChecked == true;
            bool userHasActiveRent = chkActiveRent.IsChecked == true;

            viewModel.UpdateCarRentVar(carExists, carAvailable, userLoggedIn, userHasActiveRent);

            bool isPreConditionMet = viewModel.GetCarRentPre();

            if (isPreConditionMet)
            {
                preIndicator.Fill = Brushes.Green;
            }
            else
            {
                preIndicator.Fill = Brushes.Red;
            }
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

            // Проверка предусловия
            if (!viewModel.GetCarRentPre())
            {
                MessageBox.Show("Предусловие не выполнено — операция не может быть выполнена.",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            // Выполнение операции
            viewModel.ExecuteCarRent();

            // Обновление галочек и индикаторов в интерфейсе
            chkCarAvailable.IsChecked = false;
            chkActiveRent.IsChecked = true;
            UpdatePostIndicator();
        }

        private void UpdatePostIndicator()
        {
            postIndicator.Fill = viewModel.GetCarRentPost()
                ? Brushes.Green
                : Brushes.Red;
        }
    }
}