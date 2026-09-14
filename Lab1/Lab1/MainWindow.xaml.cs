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
                }
            }
        }

        private void CheckBox_StateChanged(object sender, RoutedEventArgs e)
        {
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

        }
    }
}