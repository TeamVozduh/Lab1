using NUnit.Framework;
using Lab1.Contracts;
using Lab1;
using System.Collections.Generic;

namespace UnitTests
{

    [TestFixture]
    public class ContractRegistryTests
    {
        [Test]
        public void RentCarKey_HasCorrectValue()
        {
            Assert.That(ContractRegistry.RentCarKey, Is.EqualTo("Начать аренду"));
        }

        [Test]
        public void ReturnCarKey_HasCorrectValue()
        {
            Assert.That(ContractRegistry.ReturnCarKey, Is.EqualTo("Завершить аренду"));
        }

        [Test]
        public void TopUpKey_HasCorrectValue()
        {
            Assert.That(ContractRegistry.TopUpKey, Is.EqualTo("Пополнить баланс"));
        }

        //get

        [Test]
        public void Get_RentCarKey_ReturnsContractInfo()
        {
            var info = ContractRegistry.Get(ContractRegistry.RentCarKey);

            Assert.Multiple(() =>
            {
                Assert.That(info, Is.Not.Null);
                Assert.That(info.Title, Does.Contain("Начать аренду"));
                Assert.That(info.Pre, Is.Not.Empty);
                Assert.That(info.Post, Is.Not.Empty);
            });
        }

        [Test]
        public void Get_ReturnCarKey_ReturnsContractInfo()
        {
            var info = ContractRegistry.Get(ContractRegistry.ReturnCarKey);

            Assert.Multiple(() =>
            {
                Assert.That(info, Is.Not.Null);
                Assert.That(info.Title, Does.Contain("Завершить аренду"));
            });
        }

        [Test]
        public void Get_TopUpKey_ReturnsContractInfo()
        {
            var info = ContractRegistry.Get(ContractRegistry.TopUpKey);

            Assert.Multiple(() =>
            {
                Assert.That(info, Is.Not.Null);
                Assert.That(info.Title, Does.Contain("Пополнить баланс"));
            });
        }

        [Test]
        public void Get_UnknownKey_ThrowsKeyNotFoundException()
        {
            Assert.Throws<KeyNotFoundException>(() =>
                ContractRegistry.Get("Несуществующий ключ"));
        }

        [Test]
        public void Get_EmptyKey_ThrowsKeyNotFoundException()
        {
            Assert.Throws<KeyNotFoundException>(() =>
                ContractRegistry.Get(""));
        }

        //try get

        [Test]
        public void TryGet_ExistingKey_ReturnsTrueAndInfo()
        {
            bool result = ContractRegistry.TryGet(ContractRegistry.RentCarKey, out var info);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.True);
                Assert.That(info, Is.Not.Null);
            });
        }

        [Test]
        public void TryGet_UnknownKey_ReturnsFalseAndNull()
        {
            bool result = ContractRegistry.TryGet("Несуществующий", out var info);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(info, Is.Null);
            });
        }

        [Test]
        public void TryGet_AllKeys_ReturnsTrue()
        {
            Assert.Multiple(() =>
            {
                Assert.That(ContractRegistry.TryGet(ContractRegistry.RentCarKey, out _), Is.True);
                Assert.That(ContractRegistry.TryGet(ContractRegistry.ReturnCarKey, out _), Is.True);
                Assert.That(ContractRegistry.TryGet(ContractRegistry.TopUpKey, out _), Is.True);
            });
        }

        //контракты

        [Test]
        public void RentCarContract_HasAllFieldsFilled()
        {
            var info = ContractRegistry.Get(ContractRegistry.RentCarKey);

            Assert.Multiple(() =>
            {
                Assert.That(info.Title, Is.Not.Null.And.Not.Empty);
                Assert.That(info.Pre, Is.Not.Null.And.Not.Empty);
                Assert.That(info.Post, Is.Not.Null.And.Not.Empty);
                Assert.That(info.Effects, Is.Not.Null.And.Not.Empty);
                Assert.That(info.Exceptions, Is.Not.Null.And.Not.Empty);
                Assert.That(info.CorrectExample, Is.Not.Null.And.Not.Empty);
                Assert.That(info.IncorrectExample, Is.Not.Null.And.Not.Empty);
            });
        }

        [Test]
        public void ReturnCarContract_HasAllFieldsFilled()
        {
            var info = ContractRegistry.Get(ContractRegistry.ReturnCarKey);

            Assert.Multiple(() =>
            {
                Assert.That(info.Title, Is.Not.Null.And.Not.Empty);
                Assert.That(info.Pre, Is.Not.Null.And.Not.Empty);
                Assert.That(info.Post, Is.Not.Null.And.Not.Empty);
                Assert.That(info.Effects, Is.Not.Null.And.Not.Empty);
                Assert.That(info.Exceptions, Is.Not.Null.And.Not.Empty);
                Assert.That(info.CorrectExample, Is.Not.Null.And.Not.Empty);
                Assert.That(info.IncorrectExample, Is.Not.Null.And.Not.Empty);
            });
        }

        [Test]
        public void TopUpContract_HasAllFieldsFilled()
        {
            var info = ContractRegistry.Get(ContractRegistry.TopUpKey);

            Assert.Multiple(() =>
            {
                Assert.That(info.Title, Is.Not.Null.And.Not.Empty);
                Assert.That(info.Pre, Is.Not.Null.And.Not.Empty);
                Assert.That(info.Post, Is.Not.Null.And.Not.Empty);
                Assert.That(info.Effects, Is.Not.Null.And.Not.Empty);
                Assert.That(info.Exceptions, Is.Not.Null.And.Not.Empty);
                Assert.That(info.CorrectExample, Is.Not.Null.And.Not.Empty);
                Assert.That(info.IncorrectExample, Is.Not.Null.And.Not.Empty);
            });
        }

        [Test]
        public void RentCarContract_PreContainsCarAvailable()
        {
            var info = ContractRegistry.Get(ContractRegistry.RentCarKey);
            Assert.That(info.Pre, Does.Contain("Автомобиль доступен"));
        }

        [Test]
        public void ReturnCarContract_PreContainsDistanceAndTariff()
        {
            var info = ContractRegistry.Get(ContractRegistry.ReturnCarKey);
            Assert.Multiple(() =>
            {
                Assert.That(info.Pre, Does.Contain("расстояние"));
                Assert.That(info.Pre, Does.Contain("тариф"));
            });
        }

        [Test]
        public void TopUpContract_PreContainsTopUpValue()
        {
            var info = ContractRegistry.Get(ContractRegistry.TopUpKey);
            Assert.That(info.Pre, Does.Contain("сумма пополнения"));
        }

        [Test]
        public void AllContracts_HaveExceptionsDescription()
        {
            var rent = ContractRegistry.Get(ContractRegistry.RentCarKey);
            var ret = ContractRegistry.Get(ContractRegistry.ReturnCarKey);
            var topUp = ContractRegistry.Get(ContractRegistry.TopUpKey);

            Assert.Multiple(() =>
            {
                Assert.That(rent.Exceptions, Does.Contain("GuardViolationException"));
                Assert.That(ret.Exceptions, Does.Contain("GuardViolationException"));
                Assert.That(topUp.Exceptions, Does.Contain("GuardViolationException"));
            });
        }

        [Test]
        public void AllContracts_HaveCorrectAndIncorrectExamples()
        {
            var rent = ContractRegistry.Get(ContractRegistry.RentCarKey);
            var ret = ContractRegistry.Get(ContractRegistry.ReturnCarKey);
            var topUp = ContractRegistry.Get(ContractRegistry.TopUpKey);

            Assert.Multiple(() =>
            {
                Assert.That(rent.CorrectExample, Is.Not.Empty);
                Assert.That(rent.IncorrectExample, Is.Not.Empty);
                Assert.That(ret.CorrectExample, Is.Not.Empty);
                Assert.That(ret.IncorrectExample, Is.Not.Empty);
                Assert.That(topUp.CorrectExample, Is.Not.Empty);
                Assert.That(topUp.IncorrectExample, Is.Not.Empty);
            });
        }
    }


    [TestFixture]
    public class ContractInfoTests
    {
        [Test]
        public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
        {
            var info = new ContractInfo(
                Title: "Тестовый контракт",
                Pre: "Предусловие",
                Post: "Постусловие",
                Effects: "Эффекты",
                Exceptions: "Исключения",
                CorrectExample: "Правильный пример",
                IncorrectExample: "Неправильный пример");

            Assert.Multiple(() =>
            {
                Assert.That(info.Title, Is.EqualTo("Тестовый контракт"));
                Assert.That(info.Pre, Is.EqualTo("Предусловие"));
                Assert.That(info.Post, Is.EqualTo("Постусловие"));
                Assert.That(info.Effects, Is.EqualTo("Эффекты"));
                Assert.That(info.Exceptions, Is.EqualTo("Исключения"));
                Assert.That(info.CorrectExample, Is.EqualTo("Правильный пример"));
                Assert.That(info.IncorrectExample, Is.EqualTo("Неправильный пример"));
            });
        }

        [Test]
        public void Record_Equality_Works()
        {
            var info1 = new ContractInfo("A", "B", "C", "D", "E", "F", "G");
            var info2 = new ContractInfo("A", "B", "C", "D", "E", "F", "G");

            Assert.That(info1, Is.EqualTo(info2));
        }

        [Test]
        public void Record_Inequality_Works()
        {
            var info1 = new ContractInfo("A", "B", "C", "D", "E", "F", "G");
            var info2 = new ContractInfo("A", "B", "C", "D", "E", "F", "H");

            Assert.That(info1, Is.Not.EqualTo(info2));
        }

        [Test]
        public void ToString_ReturnsNonEmptyString()
        {
            var info = new ContractInfo("A", "B", "C", "D", "E", "F", "G");
            Assert.That(info.ToString(), Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Record_WithExpression_CreatesCopy()Ы
        {
            var info1 = new ContractInfo("A", "B", "C", "D", "E", "F", "G");
            var info2 = info1 with { Title = "Новый заголовок" };

            Assert.Multiple(() =>
            {
                Assert.That(info2.Title, Is.EqualTo("Новый заголовок"));
                Assert.That(info1.Title, Is.EqualTo("A")); 
            });
        }
    }
}