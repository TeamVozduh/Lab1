using NUnit.Framework;
using Lab1;
using Lab1.Contracts;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace UnitTests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainWindowUITests
    {
        private MainWindow window;

        [SetUp]
        public void Setup()
        {
            window = new MainWindow();
        }

        [TearDown]
        public void TearDown()
        {
            window?.Close();
        }

        [Test]
        public void Constructor_CreatesWindow()
        {
            Assert.That(window, Is.Not.Null);
        }

        [Test]
        public void Constructor_OperationsListBox_HasThreeItems()
        {
            Assert.That(window.operationsListBox.Items.Count, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_OperationsListBox_ContainsAllKeys()
        {
            Assert.Multiple(() =>
            {
                Assert.That(window.operationsListBox.Items.Contains(ContractRegistry.RentCarKey), Is.True);
                Assert.That(window.operationsListBox.Items.Contains(ContractRegistry.ReturnCarKey), Is.True);
                Assert.That(window.operationsListBox.Items.Contains(ContractRegistry.TopUpKey), Is.True);
            });
        }

        [Test]
        public void Constructor_ParameterGroups_AreCollapsed()
        {
            Assert.Multiple(() =>
            {
                Assert.That(window.rentParametersGroup.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(window.returnParametersGroup.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(window.topUpParametersGroup.Visibility, Is.EqualTo(Visibility.Collapsed));
            });
        }

        [Test]
        public void Constructor_Indicators_AreLightGray()
        {
            Assert.Multiple(() =>
            {
                Assert.That(window.preIndicator.Fill, Is.EqualTo(Brushes.LightGray));
                Assert.That(window.postIndicator.Fill, Is.EqualTo(Brushes.LightGray));
            });
        }

        [Test]
        public void Constructor_ResultTextBox_IsEmpty()
        {
            Assert.That(window.resultTextBox.Text, Is.Empty);
        }

        [Test]
        public void SelectRentCar_ShowsRentGroup()
        {
            window.operationsListBox.SelectedItem = ContractRegistry.RentCarKey;

            Assert.Multiple(() =>
            {
                Assert.That(window.rentParametersGroup.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(window.returnParametersGroup.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(window.topUpParametersGroup.Visibility, Is.EqualTo(Visibility.Collapsed));
            });
        }

        [Test]
        public void SelectReturnCar_ShowsReturnGroup()
        {
            window.operationsListBox.SelectedItem = ContractRegistry.ReturnCarKey;

            Assert.Multiple(() =>
            {
                Assert.That(window.returnParametersGroup.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(window.rentParametersGroup.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(window.topUpParametersGroup.Visibility, Is.EqualTo(Visibility.Collapsed));
            });
        }

        [Test]
        public void SelectTopUp_ShowsTopUpGroup()
        {
            window.operationsListBox.SelectedItem = ContractRegistry.TopUpKey;

            Assert.Multiple(() =>
            {
                Assert.That(window.topUpParametersGroup.Visibility, Is.EqualTo(Visibility.Visible));
                Assert.That(window.rentParametersGroup.Visibility, Is.EqualTo(Visibility.Collapsed));
                Assert.That(window.returnParametersGroup.Visibility, Is.EqualTo(Visibility.Collapsed));
            });
        }

        [Test]
        public void SelectRentCar_SetsParametersHeader()
        {
            window.operationsListBox.SelectedItem = ContractRegistry.RentCarKey;

            Assert.That(window.parametersHeader.Text, Does.Contain("Начать аренду"));
        }

        [Test]
        public void RentPreIndicator_ValidState_Green()
        {
            window.operationsListBox.SelectedItem = ContractRegistry.RentCarKey;
            window.chkCarAvailable.IsChecked = true;
            window.chkUserLoggedIn.IsChecked = true;
            window.chkActiveRent.IsChecked = false;

            Assert.That(window.preIndicator.Fill, Is.EqualTo(Brushes.Green));
        }

        [Test]
        public void RentPreIndicator_InvalidState_Red()
        {
            window.operationsListBox.SelectedItem = ContractRegistry.RentCarKey;
            window.chkCarAvailable.IsChecked = false;
            window.chkUserLoggedIn.IsChecked = true;
            window.chkActiveRent.IsChecked = false;

            Assert.That(window.preIndicator.Fill, Is.EqualTo(Brushes.Red));
        }

        [Test]
        public void ReturnPreIndicator_ValidState_Green()
        {
            window.operationsListBox.SelectedItem = ContractRegistry.ReturnCarKey;
            window.chkReturnUserLoggedIn.IsChecked = true;
            window.chkReturnActiveRent.IsChecked = true;
            window.txtBalance.Text = "1000";
            window.txtTariff.Text = "50";
            window.txtDistance.Text = "10";

            Assert.That(window.preIndicator.Fill, Is.EqualTo(Brushes.Green));
        }

        [Test]
        public void ReturnPreIndicator_NoActiveRent_Red()
        {
            window.operationsListBox.SelectedItem = ContractRegistry.ReturnCarKey;
            window.chkReturnUserLoggedIn.IsChecked = true;
            window.chkReturnActiveRent.IsChecked = false;
            window.txtBalance.Text = "1000";
            window.txtTariff.Text = "50";
            window.txtDistance.Text = "10";

            Assert.That(window.preIndicator.Fill, Is.EqualTo(Brushes.Red));
        }

        [Test]
        public void TopUpPreIndicator_ValidState_Green()
        {
            window.operationsListBox.SelectedItem = ContractRegistry.TopUpKey;
            window.chkTopUpUserLoggedIn.IsChecked = true;
            window.txtTopUpBalance.Text = "1000";
            window.txtTopUpValue.Text = "500";

            Assert.That(window.preIndicator.Fill, Is.EqualTo(Brushes.Green));
        }

        [Test]
        public void TopUpPreIndicator_NotLoggedIn_Red()
        {
            window.operationsListBox.SelectedItem = ContractRegistry.TopUpKey;
            window.chkTopUpUserLoggedIn.IsChecked = false;
            window.txtTopUpBalance.Text = "1000";
            window.txtTopUpValue.Text = "500";

            Assert.That(window.preIndicator.Fill, Is.EqualTo(Brushes.Red));
        }

        [Test]
        public void ResetButton_EnablesAllParameters()
        {
            var setRentDisabled = typeof(MainWindow).GetMethod("SetRentParametersEnabled",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            setRentDisabled?.Invoke(window, new object[] { false });

            Assert.That(window.chkCarAvailable.IsEnabled, Is.False);

            var resetMethod = typeof(MainWindow).GetMethod("ResetButton_Click",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            resetMethod?.Invoke(window, new object[] { null, null });

            Assert.That(window.chkCarAvailable.IsEnabled, Is.True);
        }

        [Test]
        public void SetRentParametersEnabled_False_DisablesControls()
        {
            var method = typeof(MainWindow).GetMethod("SetRentParametersEnabled",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(window, new object[] { false });

            Assert.Multiple(() =>
            {
                Assert.That(window.chkCarAvailable.IsEnabled, Is.False);
                Assert.That(window.chkUserLoggedIn.IsEnabled, Is.False);
                Assert.That(window.chkActiveRent.IsEnabled, Is.False);
            });
        }

        [Test]
        public void SetReturnParametersEnabled_False_DisablesControls()
        {
            var method = typeof(MainWindow).GetMethod("SetReturnParametersEnabled",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(window, new object[] { false });

            Assert.Multiple(() =>
            {
                Assert.That(window.chkReturnUserLoggedIn.IsEnabled, Is.False);
                Assert.That(window.chkReturnActiveRent.IsEnabled, Is.False);
                Assert.That(window.txtBalance.IsEnabled, Is.False);
                Assert.That(window.txtTariff.IsEnabled, Is.False);
                Assert.That(window.txtDistance.IsEnabled, Is.False);
            });
        }

        [Test]
        public void SetTopUpParametersEnabled_False_DisablesControls()
        {
            var method = typeof(MainWindow).GetMethod("SetTopUpParametersEnabled",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(window, new object[] { false });

            Assert.Multiple(() =>
            {
                Assert.That(window.chkTopUpUserLoggedIn.IsEnabled, Is.False);
                Assert.That(window.txtTopUpBalance.IsEnabled, Is.False);
                Assert.That(window.txtTopUpValue.IsEnabled, Is.False);
            });
        }

        [Test]
        public void RefreshPreIndicator_NoSelection_DoesNothing()
        {
            window.operationsListBox.SelectedItem = null;

            var method = typeof(MainWindow).GetMethod("RefreshPreIndicator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.DoesNotThrow(() => method?.Invoke(window, null));
        }
    }

    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ContractWindowUITests
    {
        [Test]
        public void DefaultConstructor_CreatesWindow()
        {
            var window = new ContractWindow();
            Assert.That(window, Is.Not.Null);
            window.Close();
        }

        [Test]
        public void Constructor_WithRentCarKey_BindsContractInfo()
        {
            var window = new ContractWindow(ContractRegistry.RentCarKey);

            Assert.Multiple(() =>
            {
                Assert.That(window.contractHeader.Text, Does.Contain("Начать аренду"));
                Assert.That(window.preBlock.Text, Is.Not.Empty);
                Assert.That(window.postBlock.Text, Is.Not.Empty);
            });

            window.Close();
        }

        [Test]
        public void Constructor_WithReturnCarKey_BindsContractInfo()
        {
            var window = new ContractWindow(ContractRegistry.ReturnCarKey);
            Assert.That(window.contractHeader.Text, Does.Contain("Завершить аренду"));
            window.Close();
        }

        [Test]
        public void Constructor_WithTopUpKey_BindsContractInfo()
        {
            var window = new ContractWindow(ContractRegistry.TopUpKey);
            Assert.That(window.contractHeader.Text, Does.Contain("Пополнить баланс"));
            window.Close();
        }

        [Test]
        public void Constructor_WithInvalidKey_ThrowsException()
        {
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() =>
                new ContractWindow("Несуществующий ключ"));
        }
    }
}