using System.Windows;
using System.Windows.Controls;

namespace Lab1
{
    public partial class MainWindow : Window
    {
        private ViewModel viewModel;
        public MainWindow()
        {
            InitializeComponent();
            viewModel = new ViewModel();

            operationsListBox.Items.Add("Операция 1");
            operationsListBox.Items.Add("Операция 2");
            operationsListBox.Items.Add("Операция 3");
        }

        private void OperationsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = operationsListBox.SelectedItem as string;

            if (string.IsNullOrEmpty(selected))
                parametersHeader.Text = "Ввод параметров";
            else
                parametersHeader.Text = "Ввод параметров. " + selected;
        }
    }
}