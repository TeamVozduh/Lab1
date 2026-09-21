using ClassLibrary;

namespace Lab1
{
    class ViewModel
    {
        private readonly CarRentOperation carRentOperation;

        public ViewModel()
        {
            carRentOperation = new CarRentOperation();
        }

        // --- Проброс read-only состояния модели ---
        public bool CarExists => carRentOperation.CarExists;
        public bool CarAvailable => carRentOperation.CarAvailable;
        public bool UserLoggedIn => carRentOperation.UserLoggedIn;
        public bool UserHasActiveRent => carRentOperation.UserHasActiveRent;

        public void SetParameters(bool carExists, bool carAvailable, bool userLoggedIn, bool userHasActiveRent)
        {
            carRentOperation.SetState(carExists, carAvailable, userLoggedIn, userHasActiveRent);
        }

        public bool GetCarRentPre()
        {
            return carRentOperation.getPre();
        }

        public bool GetCarRentPost()
        {
            return carRentOperation.getPost();
        }

        public void ExecuteCarRent()
        {
            bool pre = carRentOperation.getPre();
            Guard.Requires(pre, "ExecuteCarRent вызван при невыполненном предусловии");

            carRentOperation.RentCar();
        }
    }
}