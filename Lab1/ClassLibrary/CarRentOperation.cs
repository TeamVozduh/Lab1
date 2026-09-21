using System.Diagnostics;

namespace ClassLibrary
{
    public class CarRentOperation
    {
        private bool carExists;
        private bool carAvailable;
        private bool userLoggedIn;
        private bool userHasActiveRent;

        public bool CarExists => carExists;
        public bool CarAvailable => carAvailable;
        public bool UserLoggedIn => userLoggedIn;
        public bool UserHasActiveRent => userHasActiveRent;

        public CarRentOperation() { }

        public void SetState(bool carExists, bool carAvailable, bool userLoggedIn, bool userHasActiveRent)
        {
            this.carExists = carExists;
            this.carAvailable = carAvailable;
            this.userLoggedIn = userLoggedIn;
            this.userHasActiveRent = userHasActiveRent;
        }

        public bool getPre()
        {
            return carExists && carAvailable && userLoggedIn && !userHasActiveRent;
        }

        public bool getPost()
        {
            return !carAvailable && userHasActiveRent;
        }

        public void RentCar()
        {
            bool pre = getPre();

            Guard.Requires(pre, "Нарушено предусловие RentCar: " +
                                "carExists && carAvailable && userLoggedIn && !userHasActiveRent");

            carAvailable = true;
            userHasActiveRent = true;

            bool post = getPost();

            Debug.Assert(post, "Нарушено постусловие RentCar: " +
                               "!carAvailable && userHasActiveRent");
        }
    }
}