using NUnit.Framework;
using ClassLibrary;

namespace UnitTests
{
    //guard
    [TestFixture]
    public class GuardTests
    {
        [Test]
        public void Requires_WhenConditionTrue_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => Guard.Requires(true, "Ошибка"));
        }

        [Test]
        public void Requires_WhenConditionFalse_ThrowsInvalidOperationException()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                Guard.Requires(false, "Тестовое сообщение"));

            Assert.That(ex.Message, Is.EqualTo("Тестовое сообщение"));
        }
    }

    //car rent

    [TestFixture]
    public class CarRentOperationTests
    {
        private CarRentOperation rent;

        [SetUp]
        public void Setup()
        {
            rent = new CarRentOperation();
            rent = new CarRentOperation();
        }

        [Test]
        public void GetPre_AllConditionsTruereturnsTrue()
        {
            rent.SetState(carAvailable: true, userLoggedIn: true, userHasActiveRent: false);
            Assert.That(rent.GetPre(), Is.True);
        }

        [Test]
        public void GetPre_CarNotAvailablereturnsFalse()
        {
            rent.SetState(carAvailable: false, userLoggedIn: true, userHasActiveRent: false);
            Assert.That(rent.GetPre(), Is.False);
        }

        [Test]
        public void GetPre_UserNotLoggedInreturnsFalse()
        {
            rent.SetState(carAvailable: true, userLoggedIn: false, userHasActiveRent: false);
            Assert.That(rent.GetPre(), Is.False);
        }

        [Test]
        public void GetPre_UserHasActiveRentreturnsFalse()
        {
            rent.SetState(carAvailable: true, userLoggedIn: true, userHasActiveRent: true);
            Assert.That(rent.GetPre(), Is.False);
        }

        [Test]
        public void RentCar_ValidState_ChangesCarAndRentStatus()
        {
            rent.SetState(carAvailable: true, userLoggedIn: true, userHasActiveRent: false);

            rent.RentCar();

            Assert.Multiple(() =>
            {
                Assert.That(rent.CarAvailable, Is.False);
                Assert.That(rent.UserHasActiveRent, Is.True);
            });
        }

        [Test]
        public void RentCar_InvalidState_ThrowsException()
        {
            rent.SetState(carAvailable: false, userLoggedIn: true, userHasActiveRent: false);

            Assert.Throws<InvalidOperationException>(() => rent.RentCar());
        }

        [Test]
        public void GetPost_AfterRentreturnsTrue()
        {
            rent.SetState(carAvailable: true, userLoggedIn: true, userHasActiveRent: false);
            rent.RentCar();

            Assert.That(rent.GetPost(), Is.True);
        }
    }

    //car return

    [TestFixture]
    public class CarReturnOperationTests
    {
        private CarReturnOperation _return;

        [SetUp]
        public void Setup()
        {
            _return = new CarReturnOperation();
        }

        [Test]
        public void GetPre_AllConditionsTrue_ReturnsTrue()
        {
            _return.SetState(userHasActiveRent: true, userLoggedIn: true,
                             userBalance: 1000m, rentTariff: 50m, rentDistance: 100);
            Assert.That(_return.GetPre(), Is.True);
        }

        [Test]
        public void GetPre_NoActiveRent_ReturnsFalse()
        {
            _return.SetState(userHasActiveRent: false, userLoggedIn: true,
                             userBalance: 1000m, rentTariff: 50m, rentDistance: 100);
            Assert.That(_return.GetPre(), Is.False);
        }

        [Test]
        public void GetPre_ZeroDistance_ReturnsFalse()
        {
            _return.SetState(userHasActiveRent: true, userLoggedIn: true,
                             userBalance: 1000m, rentTariff: 50m, rentDistance: 0);
            Assert.That(_return.GetPre(), Is.False);
        }

        [Test]
        public void GetPre_ZeroTariff_ReturnsFalse()
        {
            _return.SetState(userHasActiveRent: true, userLoggedIn: true,
                             userBalance: 1000m, rentTariff: 0m, rentDistance: 100);
            Assert.That(_return.GetPre(), Is.False);
        }

        [Test]
        public void ReturnCar_ValidState_DecreasesBalanceAndClosesRent()
        {
            _return.SetState(userHasActiveRent: true, userLoggedIn: true,
                             userBalance: 1000m, rentTariff: 50m, rentDistance: 10);

            _return.ReturnCar();

            Assert.Multiple(() =>
            {
                Assert.That(_return.UserBalance, Is.EqualTo(500m));
                Assert.That(_return.UserHasActiveRent, Is.False);
            });
        }

        [Test]
        public void ReturnCar_InvalidState_ThrowsException()
        {
            _return.SetState(userHasActiveRent: false, userLoggedIn: true,
                             userBalance: 1000m, rentTariff: 50m, rentDistance: 100);

            Assert.Throws<InvalidOperationException>(() => _return.ReturnCar());
        }

        [Test]
        public void GetPost_AfterReturn_ReturnsTrue()
        {
            _return.SetState(userHasActiveRent: true, userLoggedIn: true,
                             userBalance: 1000m, rentTariff: 50m, rentDistance: 10);
            _return.ReturnCar();

            Assert.That(_return.GetPost(), Is.True);
        }
    }

    //balance
    [TestFixture]
    public class BalanceTopUpOperationTests
    {
        private BalanceTopUpOperation topUp;

        [SetUp]
        public void Setup()
        {
            topUp = new BalanceTopUpOperation();
        }

        [Test]
        public void GetPre_AllConditionsTruereturnsTrue()
        {
            topUp.SetState(userLoggedIn: true, userBalance: 1000m, topUpValue: 500m);
            Assert.That(topUp.GetPre(), Is.True);
        }

        [Test]
        public void GetPre_UserNotLoggedInreturnsFalse()
        {
            topUp.SetState(userLoggedIn: false, userBalance: 1000m, topUpValue: 500m);
            Assert.That(topUp.GetPre(), Is.False);
        }

        [Test]
        public void GetPre_ZeroTopUpValuereturnsFalse()
        {
            topUp.SetState(userLoggedIn: true, userBalance: 1000m, topUpValue: 0m);
            Assert.That(topUp.GetPre(), Is.False);
        }

        [Test]
        public void GetPre_NegativeTopUpValuereturnsFalse()
        {
            topUp.SetState(userLoggedIn: true, userBalance: 1000m, topUpValue: -500m);
            Assert.That(topUp.GetPre(), Is.False);
        }

        [Test]
        public void TopUp_ValidState_IncreasesBalance()
        {
            topUp.SetState(userLoggedIn: true, userBalance: 1000m, topUpValue: 500m);

            topUp.TopUp();

            Assert.That(topUp.UserBalance, Is.EqualTo(1500m));
        }

        [Test]
        public void TopUp_InvalidState_ThrowsException()
        {
            topUp.SetState(userLoggedIn: false, userBalance: 1000m, topUpValue: 500m);

            Assert.Throws<InvalidOperationException>(() => topUp.TopUp());
        }

        [Test]
        public void GetPost_AfterTopUpreturnsTrue()
        {
            topUp.SetState(userLoggedIn: true, userBalance: 1000m, topUpValue: 500m);
            topUp.TopUp();

            Assert.That(topUp.GetPost(), Is.True);
        }
    }
}