using ClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab1
{
    class ViewModel
    {
        private CarRentOperation carRentOperation;

        public ViewModel()
        {
            carRentOperation = new CarRentOperation();
        }

        public void UpdateCarRentVar(bool carExists, bool carAvailable, bool userLoggedIn, bool userHasActiveRent)
        {
            carRentOperation.carExists = carExists;
            carRentOperation.carAvailable = carAvailable;
            carRentOperation.userLoggedIn = userLoggedIn;
            carRentOperation.userHasActiveRent = userHasActiveRent;
        }
        public bool GetCarRentPre()
        {
            return carRentOperation.getPre();
        }

        public void ExecuteCarRent()
        {
            carRentOperation.RentCar(
                carRentOperation.carExists,
                carRentOperation.carAvailable,
                carRentOperation.userLoggedIn,
                carRentOperation.userHasActiveRent);
        }

        public bool GetCarRentPost()
        {
            return carRentOperation.getPost();
        }
    }
}
