using ClassLibrary;

namespace Lab1
{
    /// <summary>Connects the car-sharing UI to the three contract-aware operations.</summary>
    public class ViewModel
    {
        private readonly CarRentOperation carRentOperation;
        private readonly CarReturnOperation carReturnOperation;
        private readonly BalanceTopUpOperation balanceTopUpOperation;

        /// <summary>Creates independent operation instances for the current window.</summary>
        public ViewModel()
        {
            carRentOperation = new CarRentOperation();
            carReturnOperation = new CarReturnOperation();
            balanceTopUpOperation = new BalanceTopUpOperation();
        }


        /// <summary>Gets the car availability after the latest rental state change.</summary>
        public bool CarAvailable => carRentOperation.CarAvailable;
        /// <summary>Gets the login state used by the rental operation.</summary>
        public bool UserLoggedIn => carRentOperation.UserLoggedIn;
        /// <summary>Gets whether the rental operation has an active rental.</summary>
        public bool UserHasActiveRent => carRentOperation.UserHasActiveRent;

        /// <summary>Copies rental form values into the operation before checking its contract.</summary>
        public void SetCarRentParameters(bool carAvailable, bool userLoggedIn, bool userHasActiveRent)
        {
            carRentOperation.SetState(carAvailable, userLoggedIn, userHasActiveRent);
        }

        /// <summary>Checks whether renting is permitted in the current state.</summary>
        public bool GetCarRentPre()
        {
            return carRentOperation.GetPre();
        }

        /// <summary>Checks the rental result after execution.</summary>
        public bool GetCarRentPost()
        {
            return carRentOperation.GetPost();
        }

        /// <summary>Enforces the rental precondition and performs the operation.</summary>
        public void ExecuteCarRent()
        {
            bool pre = carRentOperation.GetPre();
            Guard.Requires(pre, "ExecuteCarRent вызван при невыполненном предусловии");

            carRentOperation.RentCar();
        }


        /// <summary>Gets the balance held by the return operation.</summary>
        public decimal UserBalance => carReturnOperation.UserBalance;
        /// <summary>Gets the tariff used by the return operation.</summary>
        public decimal RentTariff => carReturnOperation.RentTariff;
        /// <summary>Gets the distance used by the return operation.</summary>
        public int RentDistance => carReturnOperation.RentDistance;

        /// <summary>Copies return form values into the operation before contract checks.</summary>
        public void SetCarReturnParameters(bool userLoggedIn, bool userHasActiveRent,
                                           decimal balance, decimal tariff, int distance)
        {
            carReturnOperation.SetState(userHasActiveRent, userLoggedIn,
                                        balance, tariff, distance);
        }

        /// <summary>Checks whether the active rental can be closed.</summary>
        public bool GetCarReturnPre()
        {
            return carReturnOperation.GetPre();
        }

        /// <summary>Checks the return result after execution.</summary>
        public bool GetCarReturnPost()
        {
            return carReturnOperation.GetPost();
        }

        /// <summary>Enforces the return precondition and closes the rental.</summary>
        public void ExecuteCarReturn()
        {
            bool pre = carReturnOperation.GetPre();
            Guard.Requires(pre, "ExecuteCarReturn вызван при невыполненном предусловии");

            carReturnOperation.ReturnCar();
        }

        /// <summary>Gets the balance held by the top-up operation.</summary>
        public decimal TopUpBalance => balanceTopUpOperation.UserBalance;
        /// <summary>Gets the amount requested for the top-up operation.</summary>
        public decimal TopUpValue => balanceTopUpOperation.TopUpValue;

        /// <summary>Copies top-up form values into the operation before contract checks.</summary>
        public void SetBalanceTopUpParameters(bool userLoggedIn, decimal balance, decimal topUpValue)
        {
            balanceTopUpOperation.SetState(userLoggedIn, balance, topUpValue);
        }

        /// <summary>Checks whether the requested top-up is permitted.</summary>
        public bool GetBalanceTopUpPre()
        {
            return balanceTopUpOperation.GetPre();
        }

        /// <summary>Checks that the balance changed as required by the contract.</summary>
        public bool GetBalanceTopUpPost()
        {
            return balanceTopUpOperation.GetPost();
        }

        /// <summary>Enforces the top-up precondition and updates the balance.</summary>
        public void ExecuteBalanceTopUp()
        {
            bool pre = balanceTopUpOperation.GetPre();
            Guard.Requires(pre, "ExecuteBalanceTopUp вызван при невыполненном предусловии");

            balanceTopUpOperation.TopUp();
        }
    }
}