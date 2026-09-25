using Lab1.Contracts;
using System.Windows;

namespace Lab1
{
    public partial class ContractWindow : Window
    {
        public ContractWindow()
        {
            InitializeComponent();
        }

        public ContractWindow(string contractKey) : this()
        {
            var info = ContractRegistry.Get(contractKey);
            Bind(info);
        }

        private void Bind(ContractInfo info)
        {
            
            contractHeader.Text = info.Title;
            preBlock.Text = info.Pre;
            postBlock.Text = info.Post;
            effectsBlock.Text = info.Effects;
            exceptionsBlock.Text = info.Exceptions;
            correctExampleBlock.Text = info.CorrectExample;
            incorrectExampleBlock.Text = info.IncorrectExample;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}