using Lab1.Contracts;
using System.Windows;

namespace Lab1
{
    /// <summary>Displays the selected operation's contract and boundary examples.</summary>
    public partial class ContractWindow : Window
    {
        /// <summary>Initializes the contract dialog before a contract is selected.</summary>
        public ContractWindow()
        {
            InitializeComponent();
        }

        /// <summary>Loads the contract registered under the supplied key.</summary>
        public ContractWindow(string contractKey) : this()
        {
            var info = ContractRegistry.Get(contractKey);
            Bind(info);
        }

        /// <summary>Copies contract terms and examples into their display blocks.</summary>
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

        /// <summary>Closes the contract dialog.</summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}