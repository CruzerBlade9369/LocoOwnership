using System.Linq;
using UnityEngine;
using DV.ServicePenalty;
using DV.Simulation.Cars;


namespace LocoOwnership.OwnershipHandler
{
	public class DebtHandling
	{
		public static bool IsDebtClearForBuy(TrainCar car)
		{
			var locoDebtController = LocoDebtController.Instance;

			var locoDebt = car.GetComponent<SimController>().debt;

			ExistingLocoDebt existingLocoDebt = locoDebtController.trackedLocosDebts
				.FirstOrDefault(debt => debt.locoDebtTracker == locoDebt);

			if (existingLocoDebt == null)
			{
				Debug.LogError("CheckLocoDebtBuy: Loco debt not found!");
				return false;
			}
			existingLocoDebt.UpdateDebtState();

			return existingLocoDebt.GetTotalPrice() <= 0f;
		}

		public static bool IsDebtClearForSell(TrainCar car)
		{
			var ownedCarsStateController = OwnedCarsStateController.Instance;

			// get debt
			var locoDebt = car.GetComponent<SimController>().debt;
			var existingLocoDebt = ownedCarsStateController.existingOwnedCarStates
				.FirstOrDefault(debt => debt.carDebtTrackerBase == locoDebt);
			if (existingLocoDebt == null)
			{
				Debug.LogError("CheckLocoDebtSell: Loco debt not found!");
				return false;
			}
			existingLocoDebt.UpdateDebtState();

			// If has unpaid debts or debts aren't only environmental, then don't sell loco
			if (existingLocoDebt.GetTotalPrice() > 0f) return false;
			return true;
		}
	}
}
