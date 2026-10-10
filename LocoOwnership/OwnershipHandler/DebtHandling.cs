using DV.Damage;
using DV.ServicePenalty;
using DV.Utils;
using System.Linq;
using UnityEngine;

namespace LocoOwnership.OwnershipHandler
{
	public class DebtHandling
	{
		public static void RegisterDebtToWorkTrain(TrainCar car)
		{
			var locoDebt = car.SimController.debt;
			var debts = LocoDebtController.Instance.trackedLocosDebts;

			if (locoDebt == null)
			{
				Debug.LogError($"Unexpected behavior: {car.ID} still doesn't have debt");
			}

			ExistingLocoDebt locoDebtEntry = debts.Find(debt => debt.locoDebtTracker == locoDebt);

			if (locoDebtEntry != null)
			{
				Main.DebugLog($"Preparing unregister debt for {car.ID} from DVRT");
				debts.Remove(locoDebtEntry);
				SingletonBehaviour<CareerManagerDebtController>.Instance.UnregisterDebt(locoDebtEntry);
				Main.DebugLog($"Successfully unregistered debt for {car.ID}from DVRT");
				locoDebtEntry.UpdateDebtState();
			}
			else
			{
				Debug.LogWarning($"{car.ID} does not have existing loco debt, skipping removal");
			}

			// Register to work train debt tracking
			Main.DebugLog($"Preparing to register {car.ID} as work train");
			SingletonBehaviour<OwnedCarsStateController>.Instance.RegisterCarStateTracker(car, locoDebt);
			Main.DebugLog($"Registered {car.ID} as work train");
		}

		public static void RegisterDebtToDVRT(TrainCar car)
		{
			var locoDebt = car.SimController.debt;
			var debts = OwnedCarsStateController.Instance.existingOwnedCarStates;

			if (locoDebt == null)
			{
				Debug.LogError($"Debt for {car.ID} is missing for some reason, cannot continue work train debt removal");
				return;
			}

			ExistingOwnedCarDebt locoDebtEntry = debts.Find(debt => debt.carDebtTrackerBase == locoDebt);

			if (locoDebtEntry != null)
			{
				Main.DebugLog($"Preparing unregister existing debt for {car.ID} from work trains");
				debts.Remove(locoDebtEntry);
				Main.DebugLog($"Successfully unregistered debt for {car.ID}from work trains");
				locoDebtEntry.UpdateDebtState();
			}
			else
			{
				Debug.LogWarning($"{car.ID} does not have existing owned car debt, skipping removal");
			}

			// Register to regular DVRT debt
			Main.DebugLog($"Preparing to register {car.ID} as DVRT");
			SingletonBehaviour<LocoDebtController>.Instance.RegisterLocoDebtTracker(car, locoDebt);
			Main.DebugLog($"Registered {car.ID} as DVRT");
		}

		public static bool IsDebtClearForBuy(TrainCar car)
		{
			var locoDebtController = LocoDebtController.Instance;

			var locoDebt = car.SimController.debt;

			// Create debt if loco doesn't have one
			if (locoDebt == null)
			{
				Debug.LogWarning($"{car.ID} has no debt, creating one");
				car.SimController.debt = CreateNewDebt(car);

				// Refresh reference
				locoDebt = car.SimController.debt;

				locoDebtController.RegisterLocoDebtTracker(car, locoDebt);
			}

			var existingLocoDebt = locoDebtController.trackedLocosDebts
				.FirstOrDefault(debt => debt.locoDebtTracker == locoDebt);

			existingLocoDebt.UpdateDebtState();

			return existingLocoDebt.GetTotalPrice() <= 0f;
		}

		public static bool IsDebtClearForSell(TrainCar car)
		{
			var ownedCarsStateController = OwnedCarsStateController.Instance;

			// Get debt
			var locoDebt = car.SimController.debt;
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

		private static SimulatedCarDebtTracker CreateNewDebt(TrainCar car)
		{
			var simController = car.SimController;
			return new SimulatedCarDebtTracker(
				car.GetComponent<DamageController>(),
				simController.resourceContainerController,
				simController.environmentDamageController,
				simController.simFlow,
				simController.train.ID,
				simController.train.carType);
		}
	}
}
