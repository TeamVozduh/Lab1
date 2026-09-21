using System.Windows;

namespace Lab1
{
    public partial class ContractWindow : Window
    {
        public ContractWindow()
        {
            InitializeComponent();
        }

        public ContractWindow(string operationName) : this()
        {
            contractHeader.Text = "Контракт операции: " + operationName;
            this.Title = "Контракт операции: " + operationName;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}