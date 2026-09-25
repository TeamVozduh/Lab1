using NUnit.Framework;
using Lab1;
using ClassLibrary;

namespace UnitTests
{
    [TestFixture]
    public class ViewModelTests
    {
        private ViewModel VM;

        [SetUp]
        public void Setup()
        {
            VM = new ViewModel();
        }

        //rent

        [Test]
        public void SetCarRentParameters_ValidState_GetCarRentPreReturnsTrue()
        {
            VM.SetCarRentParameters(carAvailable: true, userLoggedIn: true, userHasActiveRent: false);

            Assert.That(VM.GetCarRentPre(), Is.True);
        }

        [Test]
        public void SetCarRentParameters_CarNotAvailable_GetCarRentPreReturnsFalse()
        {
            VM.SetCarRentParameters(carAvailable: false, userLoggedIn: true, userHasActiveRent: false);

            Assert.That(VM.GetCarRentPre(), Is.False);
        }

        [Test]
        public void ExecuteCarRent_ValidState_ChangesState()
        {
            VM.SetCarRentParameters(carAvailable: true, userLoggedIn: true, userHasActiveRent: false);

            VM.ExecuteCarRent();

            Assert.Multiple(() =>
            {
                Assert.That(VM.CarAvailable, Is.False);
                Assert.That(VM.UserHasActiveRent, Is.True);
            });
        }

        [Test]
        public void ExecuteCarRent_InvalidState_ThrowsException()
        {
            VM.SetCarRentParameters(carAvailable: false, userLoggedIn: true, userHasActiveRent: false);

            Assert.Throws<InvalidOperationException>(() => VM.ExecuteCarRent());
        }

        [Test]
        public void GetCarRentPost_AfterRent_ReturnsTrue()
        {
            VM.SetCarRentParameters(carAvailable: true, userLoggedIn: true, userHasActiveRent: false);
            VM.ExecuteCarRent();

            Assert.That(VM.GetCarRentPost(), Is.True);
        }

        //return

        [Test]
        public void SetCarReturnParameters_ValidState_GetCarReturnPreReturnsTrue()
        {
            VM.SetCarReturnParameters(userLoggedIn: true, userHasActiveRent: true,
                                       balance: 1000m, tariff: 50m, distance: 100);

            Assert.That(VM.GetCarReturnPre(), Is.True);
        }

        [Test]
        public void SetCarReturnParameters_NoActiveRent_GetCarReturnPreReturnsFalse()
        {
            VM.SetCarReturnParameters(userLoggedIn: true, userHasActiveRent: false,
                                       balance: 1000m, tariff: 50m, distance: 100);

            Assert.That(VM.GetCarReturnPre(), Is.False);
        }

        [Test]
        public void ExecuteCarReturn_ValidState_DecreasesBalance()
        {
            VM.SetCarReturnParameters(userLoggedIn: true, userHasActiveRent: true,
                                       balance: 1000m, tariff: 50m, distance: 10);

            VM.ExecuteCarReturn();

            Assert.Multiple(() =>
            {
                Assert.That(VM.UserBalance, Is.EqualTo(500m));
                Assert.That(VM.GetCarReturnPost(), Is.True);
            });
        }

        [Test]
        public void ExecuteCarReturn_InvalidState_ThrowsException()
        {
            VM.SetCarReturnParameters(userLoggedIn: true, userHasActiveRent: false,
                                       balance: 1000m, tariff: 50m, distance: 100);

            Assert.Throws<InvalidOperationException>(() => VM.ExecuteCarReturn());
        }

        [Test]
        public void UserBalance_AfterReturn_ReturnsUpdatedBalance()
        {
            VM.SetCarReturnParameters(userLoggedIn: true, userHasActiveRent: true,
                                       balance: 2000m, tariff: 100m, distance: 5);

            VM.ExecuteCarReturn();

            Assert.That(VM.UserBalance, Is.EqualTo(1500m)); 
        }

        //balance

        [Test]
        public void SetBalanceTopUpParameters_ValidState_GetBalanceTopUpPreReturnsTrue()
        {
            VM.SetBalanceTopUpParameters(userLoggedIn: true, balance: 1000m, topUpValue: 500m);

            Assert.That(VM.GetBalanceTopUpPre(), Is.True);
        }

        [Test]
        public void SetBalanceTopUpParameters_NotLoggedIn_GetBalanceTopUpPreReturnsFalse()
        {
            VM.SetBalanceTopUpParameters(userLoggedIn: false, balance: 1000m, topUpValue: 500m);

            Assert.That(VM.GetBalanceTopUpPre(), Is.False);
        }

        [Test]
        public void ExecuteBalanceTopUp_ValidState_IncreasesBalance()
        {
            VM.SetBalanceTopUpParameters(userLoggedIn: true, balance: 1000m, topUpValue: 500m);

            VM.ExecuteBalanceTopUp();

            Assert.Multiple(() =>
            {
                Assert.That(VM.TopUpBalance, Is.EqualTo(1500m));
                Assert.That(VM.GetBalanceTopUpPost(), Is.True);
            });
        }

        [Test]
        public void ExecuteBalanceTopUp_InvalidState_ThrowsException()
        {
            VM.SetBalanceTopUpParameters(userLoggedIn: false, balance: 1000m, topUpValue: 500m);

            Assert.Throws<InvalidOperationException>(() => VM.ExecuteBalanceTopUp());
        }

        [Test]
        public void ExecuteBalanceTopUp_ZeroValue_ThrowsException()
        {
            VM.SetBalanceTopUpParameters(userLoggedIn: true, balance: 1000m, topUpValue: 0m);

            Assert.Throws<InvalidOperationException>(() => VM.ExecuteBalanceTopUp());
        }

        //full test

        [Test]
        public void FullCycle_RentAndReturn_WorksCorrectly()
        {
            // 1. Аренда
            VM.SetCarRentParameters(carAvailable: true, userLoggedIn: true, userHasActiveRent: false);
            VM.ExecuteCarRent();

            Assert.That(VM.UserHasActiveRent, Is.True);

            // 2. Возврат
            VM.SetCarReturnParameters(userLoggedIn: true, userHasActiveRent: true,
                                       balance: 1000m, tariff: 50m, distance: 10);
            VM.ExecuteCarReturn();

            Assert.Multiple(() =>
            {
                Assert.That(VM.UserBalance, Is.EqualTo(500m));
                Assert.That(VM.GetCarReturnPost(), Is.True);
            });
        }
    }
}