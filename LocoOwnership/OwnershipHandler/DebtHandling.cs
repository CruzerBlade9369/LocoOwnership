using System.Linq;
using UnityEngine;
using DV.ServicePenalty;
using DV.Simulation.Cars;


namespace LocoOwnership.OwnershipHandler
{
	public class DebtHandling
	{
		public static bool IsDebtClearForBuy(TrainCar car, TrainCar tender = null)
		{
			var locoDebtController = LocoDebtController.Instance;
			float totalDebtCheck = 0f;

			// get debt
			var locoDebt = car.GetComponent<SimController>().debt;
			ExistingLocoDebt existingLocoDebt = locoDebtController.trackedLocosDebts
				.FirstOrDefault(debt => debt.locoDebtTracker == locoDebt);
			if (existingLocoDebt == null)
			{
				Debug.LogError("CheckLocoDebtBuy: Loco debt not found!");
				return false;
			}
			existingLocoDebt.UpdateDebtState();
			totalDebtCheck += existingLocoDebt.GetTotalPrice();

			// check tender debt if present
			if (tender != null)
			{
				var tenderDebt = tender.GetComponent<SimController>().debt;
				if (tenderDebt != null)
				{
					ExistingLocoDebt existingTenderDebt = locoDebtController.trackedLocosDebts
						.FirstOrDefault(debt => debt.locoDebtTracker == tenderDebt);
					if (existingTenderDebt == null)
					{
						Debug.LogError("CheckLocoDebtBuy: Tender debt not found!");
						return false;
					}
					existingTenderDebt.UpdateDebtState();
					totalDebtCheck += existingTenderDebt.GetTotalPrice();
				}
			}

			return totalDebtCheck <= 0f;
		}

		public static bool IsDebtClearForSell(TrainCar car, TrainCar tender = null)
		{
			var ownedCarsStateController = OwnedCarsStateController.Instance;
			float totalDebtCheck = 0f;

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
			totalDebtCheck += existingLocoDebt.GetTotalPrice();

			// check tender debt if present
			if (tender != null)
			{
				var tenderDebt = tender.GetComponent<SimController>().debt;
				var existingTenderDebt = ownedCarsStateController.existingOwnedCarStates
					.FirstOrDefault(debt => debt.carDebtTrackerBase == tenderDebt);
				if (existingTenderDebt == null)
				{
					Debug.LogError("CheckLocoDebtSell: Tender debt not found!");
					return false;
				}
				existingTenderDebt.UpdateDebtState();
				totalDebtCheck += existingTenderDebt.GetTotalPrice();
			}

			// if has unpaid debts or debts arent only environmental then dont sell loco
			if (totalDebtCheck > 0f)
			{
				return false;
			}

			return true;
		}
	}
}
