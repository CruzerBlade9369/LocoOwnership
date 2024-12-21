using System;
using System.Reflection;

using DV.ServicePenalty;
using DV.Simulation.Cars;
using DV.Utils;

namespace LocoOwnership.OwnershipHandler
{
	internal class DebtHandling
	{
		public static SimulatedCarDebtTracker DebtValueStealer(SimController simController)
		{
			SimulatedCarDebtTracker debt;

			// Use reflection to steal the private 'debt' field
			if (simController == null)
			{
				throw new Exception("SimController not found on the specified TrainCar!");
			}

			FieldInfo debtField = typeof(SimController).GetField("debt",
				BindingFlags.NonPublic | BindingFlags.Instance);
			if (debtField == null)
			{
				throw new Exception("Field 'debt' not found in SimController!");
			}

			debt = (SimulatedCarDebtTracker)debtField.GetValue(simController);
			if (debt == null)
			{
				throw new Exception("Value of locoDebt is null!");
			}

			return debt;
		}

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region FOR LOCO BUY

		private static bool RemoveTrackedLocoDebts(SimulatedCarDebtTracker locoDebt, SimulatedCarDebtTracker tenderDebt)
		{
			float totalDebtCheck = 0f;

			// Steal debt controller component
			LocoDebtController locoDebtController = LocoDebtController.Instance;
			if (locoDebtController == null)
			{
				throw new Exception("LocoDebtController instance is null!");
			}

			// Find the traincar in tracked loco debts to remove
			int num = locoDebtController.trackedLocosDebts.FindIndex
				((ExistingLocoDebt debt) => debt.locoDebtTracker == locoDebt);
			if (num == -1)
			{
				throw new Exception($"RemoveTrackedLocoDebts: The index is {num}! This shouldn't happen!");
			}
			ExistingLocoDebt existingLocoDebt = locoDebtController.trackedLocosDebts[num];
			existingLocoDebt.UpdateDebtState();
			totalDebtCheck += existingLocoDebt.GetTotalPrice();

			// If tender is there find debt for tender too
			ExistingLocoDebt existingTenderDebt = null;
			int num2 = -1;
			if (tenderDebt != null)
			{
				num2 = locoDebtController.trackedLocosDebts.FindIndex
					((ExistingLocoDebt debt2) => debt2.locoDebtTracker == tenderDebt);
				if (num2 == -1)
				{
					throw new Exception($"RemoveTrackedLocoDebts: The tender index is {num2}! This shouldn't happen!");
				}
				existingTenderDebt = locoDebtController.trackedLocosDebts[num2];
				existingTenderDebt.UpdateDebtState();
				totalDebtCheck += existingTenderDebt.GetTotalPrice();
			}

			// If the (total) debt isn't 0 don't allow to buy loco
			if (totalDebtCheck > 0f)
			{
				return false;
			}

			// Remove car from tracked debts
			if (existingTenderDebt != null)
			{
				// If the tender has a smaller index than the main loco, reduce the index of the main loco by 1
				// to account for the index shift
				if (num2 < num)
				{
					num--;
				}

				Main.DebugLog("Prepare unregister tender debt");
				locoDebtController.trackedLocosDebts.RemoveAt(num2);
				SingletonBehaviour<CareerManagerDebtController>.Instance.UnregisterDebt(existingTenderDebt);
				Main.DebugLog("Unregistered tender debt");
				existingTenderDebt.UpdateDebtState();
			}
			Main.DebugLog("Prepare unregister loco debt");
			locoDebtController.trackedLocosDebts.RemoveAt(num);
			SingletonBehaviour<CareerManagerDebtController>.Instance.UnregisterDebt(existingLocoDebt);
			Main.DebugLog("Unregistered loco debt");
			existingLocoDebt.UpdateDebtState();

			return true;
		}

		public static bool SetVehicleToOwned(TrainCar car, TrainCar tender)
		{
			SimulatedCarDebtTracker locoDebt;
			SimulatedCarDebtTracker tenderDebt = null;

			// Get car's sim controller component and steal debt component
			SimController simController = car.GetComponent<SimController>();
			locoDebt = DebtValueStealer(simController);

			SimController tenderSimController = null;
			if (tender != null)
			{
				tenderSimController = tender.GetComponent<SimController>();
				tenderDebt = DebtValueStealer(tenderSimController);
			}

			// Remove car(s) from tracked loco debts
			bool success = RemoveTrackedLocoDebts(locoDebt, tenderDebt);
			if (!success)
			{
				return false;
			}

			// Invoke OnLogicCarInitialized with new uniqueCar value to register to owned vehicles list
			MethodInfo onLogicCarInitializedMethod = typeof(SimController).GetMethod("OnLogicCarInitialized",
				BindingFlags.NonPublic | BindingFlags.Instance);
			if (onLogicCarInitializedMethod != null)
			{
				if (tender != null && tenderSimController != null)
				{
					tender.uniqueCar = true;
					onLogicCarInitializedMethod.Invoke(tenderSimController, null);
					Main.DebugLog("OnLogicCarInitialized method reinvoked on tender.");
				}
				car.uniqueCar = true;
				onLogicCarInitializedMethod.Invoke(simController, null);
				Main.DebugLog("OnLogicCarInitialized method reinvoked.");
			}
			else
			{
				throw new Exception("onLogicCarInitialized method failed to be found!");
			}

			return true;
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region FOR LOCO SELL

		public static bool RemoveExistingOwnedCarState(SimulatedCarDebtTracker locoDebt, SimulatedCarDebtTracker tenderDebt)
		{
			float totalDebtCheck = 0f;

			// Find the traincar in owned car states to remove
			OwnedCarsStateController ownedCarsStateController = OwnedCarsStateController.Instance;
			if (ownedCarsStateController == null)
			{
				throw new Exception("OwnedCarsStateController instance is null!");
			}

			int num = ownedCarsStateController.existingOwnedCarStates.FindIndex
				((ExistingOwnedCarDebt debt) => debt.carDebtTrackerBase == locoDebt);
			if (num == -1)
			{
				throw new Exception($"RemoveExistingOwnedCarState: The index is {num}! This shouldn't happen!");
			}
			ExistingOwnedCarDebt existingLocoDebt = ownedCarsStateController.existingOwnedCarStates[num];
			existingLocoDebt.UpdateDebtState();

			bool isLocoDebtOnlyEnv = existingLocoDebt.carDebtTrackerBase.IsDebtOnlyEnvironmental();
			totalDebtCheck += existingLocoDebt.GetTotalPrice();

			// If tender is there find debt for tender too
			ExistingOwnedCarDebt existingTenderDebt = null;
			bool isTenderDebtOnlyEnv = false;
			int num2 = -1;
			if (tenderDebt != null)
			{
				num2 = ownedCarsStateController.existingOwnedCarStates.FindIndex
					((ExistingOwnedCarDebt debt2) => debt2.carDebtTrackerBase == tenderDebt);
				if (num2 == -1)
				{
					throw new Exception($"RemoveExistingOwnedCarState: The tender index is {num2}! This shouldn't happen!");
				}
				existingTenderDebt = ownedCarsStateController.existingOwnedCarStates[num2];
				existingTenderDebt.UpdateDebtState();

				isTenderDebtOnlyEnv = existingTenderDebt.carDebtTrackerBase.IsDebtOnlyEnvironmental();
				totalDebtCheck += existingTenderDebt.GetTotalPrice();
			}

			// If has unpaid debts or debts arent only environmental then dont sell loco
			if (existingTenderDebt != null)
			{
				if (totalDebtCheck > 0f)
				{
					if (!isLocoDebtOnlyEnv && !isTenderDebtOnlyEnv)
					{
						return false;
					}
				}
			}
			else
			{
				if (totalDebtCheck > 0f)
				{
					if (!isLocoDebtOnlyEnv)
					{
						return false;
					}
				}
			}

			if (existingTenderDebt != null)
			{
				if (num2 < num)
				{
					num--;
				}

				Main.DebugLog("Removing tender from owned cars list");
				existingTenderDebt.car.uniqueCar = false;
				ownedCarsStateController.existingOwnedCarStates.RemoveAt(num2);
				Main.DebugLog("Removed tender from owned cars list");
				existingTenderDebt.UpdateDebtState();
			}
			Main.DebugLog("Removing loco from owned cars list");
			existingLocoDebt.car.uniqueCar = false;
			ownedCarsStateController.existingOwnedCarStates.RemoveAt(num);
			Main.DebugLog("Removed loco from owned cars list");
			existingLocoDebt.UpdateDebtState();
			
			return true;
		}

		public static bool RemoveOwnedVehicle(TrainCar car, TrainCar tender)
		{
			SimulatedCarDebtTracker locoDebt;
			SimulatedCarDebtTracker tenderDebt = null;

			// Get car's sim controller component and steal debt component
			SimController simController = car.GetComponent<SimController>();
			locoDebt = DebtValueStealer(simController);

			SimController tenderSimController = null;

			if (tender != null)
			{
				tenderSimController = tender.GetComponent<SimController>();
				tenderDebt = DebtValueStealer(tenderSimController);
			}

			// Remove car(s) from tracked woned car state
			bool success = RemoveExistingOwnedCarState(locoDebt, tenderDebt);
			if (!success)
			{
				return false;
			}

			// Invoke OnLogicCarInitialized with new uniqueCar value to remove from owned vehicles list
			MethodInfo onLogicCarInitializedMethod = typeof(SimController).GetMethod("OnLogicCarInitialized",
				BindingFlags.NonPublic | BindingFlags.Instance);
			if (onLogicCarInitializedMethod != null)
			{
				if (tender != null && tenderSimController != null)
				{
					tender.uniqueCar = false;
					onLogicCarInitializedMethod.Invoke(tenderSimController, null);
					Main.DebugLog("OnLogicCarInitialized method reinvoked on tender.");
				}
				car.uniqueCar = false;
				onLogicCarInitializedMethod.Invoke(simController, null);
				Main.DebugLog("OnLogicCarInitialized method reinvoked.");
			}
			else
			{
				throw new Exception("onLogicCarInitialized method failed to be found!");
			}

			return true;
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/
	}
}
