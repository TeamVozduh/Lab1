using System.Diagnostics;

namespace ClassLibrary
{
    public class CarReturnOperation
    {
        private bool userHasActiveRent;
        private bool userLoggedIn;
        private decimal userBalance;
        private decimal rentTariff;
        private int rentDistance;

        public bool UserHasActiveRent => userHasActiveRent;
        public bool UserLoggedIn => userLoggedIn;
        public decimal UserBalance => userBalance;
        public decimal RentTariff => rentTariff;
        public int RentDistance => rentDistance;

        public CarReturnOperation() { }

        public void SetState(bool userHasActiveRent, bool userLoggedIn,
                             decimal userBalance, decimal rentTariff, int rentDistance)
        {
            this.userHasActiveRent = userHasActiveRent;
            this.userLoggedIn = userLoggedIn;
            this.userBalance = userBalance;
            this.rentTariff = rentTariff;
            this.rentDistance = rentDistance;
        }

        public bool GetPre()
        {
            return userHasActiveRent
                && userLoggedIn
                && rentDistance > 0
                && rentTariff > 0;
        }

        public bool GetPost()
        {
            return !userHasActiveRent;
        }

        public void ReturnCar()
        {
            bool pre = GetPre();

            Guard.Requires(pre, "Нарушено предусловие ReturnCar: " +
                                "userHasActiveRent && userLoggedIn && " +
                                "rentDistance > 0 && rentTariff > 0");

            decimal cost = rentDistance * rentTariff;
            userBalance -= cost;
            userHasActiveRent = false;

            bool post = GetPost();

            Debug.Assert(post, "Нарушено постусловие ReturnCar: " +
                               "!userHasActiveRent");
        }
    }
}