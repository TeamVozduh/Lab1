using System.Diagnostics;

namespace ClassLibrary
{
    public class BalanceTopUpOperation
    {
        private bool userLoggedIn;
        private decimal userBalance;
        private decimal topUpValue;

        public bool UserLoggedIn => userLoggedIn;
        public decimal UserBalance => userBalance;
        public decimal TopUpValue => topUpValue;

        public BalanceTopUpOperation() { }

        public void SetState(bool userLoggedIn, decimal userBalance, decimal topUpValue)
        {
            this.userLoggedIn = userLoggedIn;
            this.userBalance = userBalance;
            this.topUpValue = topUpValue;
        }

        public bool GetPre()
        {
            return userLoggedIn
                && topUpValue > 0;
        }

        public bool GetPost()
        {
            return userBalance >= 0;
        }

        public void TopUp()
        {
            bool pre = GetPre();

            Guard.Requires(pre, "Нарушено предусловие TopUp: " +
                                "userLoggedIn && topUpValue > 0");

            decimal balanceBefore = userBalance;
            decimal expectedBalance = balanceBefore + topUpValue;

            userBalance += topUpValue;

            bool post = GetPost() && userBalance == expectedBalance;

            Debug.Assert(post, "Нарушено постусловие TopUp: " +
                               "баланс увеличился ровно на сумму пополнения");
        }
    }
}