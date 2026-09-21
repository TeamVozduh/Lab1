namespace ClassLibrary
{
    public class CarRentOperation
    {
        public bool carExists;
        public bool carAvailable;
        public bool userLoggedIn;
        public bool userHasActiveRent;

        public CarRentOperation()
        {
            carExists = false;
            carAvailable = false;
            userLoggedIn = false;
            userHasActiveRent = false;
        }

        public bool getPre()
        {
            return carExists && carAvailable && userLoggedIn && !userHasActiveRent;
        }

        public bool getPost()
        {
            return !carAvailable && userHasActiveRent;
        }

        public void RentCar(bool carExists, bool carAvailable, bool userLoggedIn, bool userHasActiveRent)
        {
            if (carExists && carAvailable && userLoggedIn && !userHasActiveRent)
            {
                this.carAvailable = false;
                this.userHasActiveRent = true;
            }
        }
    }
}