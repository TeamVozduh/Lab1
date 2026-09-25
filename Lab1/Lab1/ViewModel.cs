using ClassLibrary;

namespace Lab1
{
    class ViewModel
    {
        private readonly CarRentOperation carRentOperation;
        private readonly CarReturnOperation carReturnOperation;
        private readonly BalanceTopUpOperation balanceTopUpOperation;

        public ViewModel()
        {
            carRentOperation = new CarRentOperation();
            carReturnOperation = new CarReturnOperation();
            balanceTopUpOperation = new BalanceTopUpOperation();
        }


        public bool CarAvailable => carRentOperation.CarAvailable;
        public bool UserLoggedIn => carRentOperation.UserLoggedIn;
        public bool UserHasActiveRent => carRentOperation.UserHasActiveRent;

        public void SetCarRentParameters(bool carAvailable, bool userLoggedIn, bool userHasActiveRent)
        {
            carRentOperation.SetState(carAvailable, userLoggedIn, userHasActiveRent);
        }

        public bool GetCarRentPre()
        {
            return carRentOperation.GetPre();
        }

        public bool GetCarRentPost()
        {
            return carRentOperation.GetPost();
        }

        public void ExecuteCarRent()
        {
            bool pre = carRentOperation.GetPre();
            Guard.Requires(pre, "ExecuteCarRent вызван при невыполненном предусловии");

            carRentOperation.RentCar();
        }


        public decimal UserBalance => carReturnOperation.UserBalance;
        public decimal RentTariff => carReturnOperation.RentTariff;
        public int RentDistance => carReturnOperation.RentDistance;

        public void SetCarReturnParameters(bool userLoggedIn, bool userHasActiveRent,
                                           decimal balance, decimal tariff, int distance)
        {
            carReturnOperation.SetState(userHasActiveRent, userLoggedIn,
                                        balance, tariff, distance);
        }

        public bool GetCarReturnPre()
        {
            return carReturnOperation.GetPre();
        }

        public bool GetCarReturnPost()
        {
            return carReturnOperation.GetPost();
        }

        public void ExecuteCarReturn()
        {
            bool pre = carReturnOperation.GetPre();
            Guard.Requires(pre, "ExecuteCarReturn вызван при невыполненном предусловии");

            carReturnOperation.ReturnCar();
        }

        public decimal TopUpBalance => balanceTopUpOperation.UserBalance;
        public decimal TopUpValue => balanceTopUpOperation.TopUpValue;

        public void SetBalanceTopUpParameters(bool userLoggedIn, decimal balance, decimal topUpValue)
        {
            balanceTopUpOperation.SetState(userLoggedIn, balance, topUpValue);
        }

        public bool GetBalanceTopUpPre()
        {
            return balanceTopUpOperation.GetPre();
        }

        public bool GetBalanceTopUpPost()
        {
            return balanceTopUpOperation.GetPost();
        }

        public void ExecuteBalanceTopUp()
        {
            bool pre = balanceTopUpOperation.GetPre();
            Guard.Requires(pre, "ExecuteBalanceTopUp вызван при невыполненном предусловии");

            balanceTopUpOperation.TopUp();
        }
    }
}