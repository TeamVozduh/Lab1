using System.Diagnostics;

namespace ClassLibrary
{
    public class CarRentOperation
    {
        private bool carAvailable;
        private bool userLoggedIn;
        private bool userHasActiveRent;

        public bool CarAvailable => carAvailable;
        public bool UserLoggedIn => userLoggedIn;
        public bool UserHasActiveRent => userHasActiveRent;

        public CarRentOperation() { }

        public void SetState(bool carAvailable, bool userLoggedIn, bool userHasActiveRent)
        {
            this.carAvailable = carAvailable;
            this.userLoggedIn = userLoggedIn;
            this.userHasActiveRent = userHasActiveRent;
        }

        public bool GetPre()
        {
            return carAvailable && userLoggedIn && !userHasActiveRent;
        }

        public bool GetPost()
        {
            return !carAvailable && userHasActiveRent;
        }

        public void RentCar()
        {
            bool pre = GetPre();

            Guard.Requires(pre, "Нарушено предусловие RentCar: " +
                                "carExists && carAvailable && userLoggedIn && !userHasActiveRent");

            carAvailable = false;
            userHasActiveRent = true;

            bool post = GetPost();

            Debug.Assert(post, "Нарушено постусловие RentCar: " +
                               "!carAvailable && userHasActiveRent");
        }
    }
}