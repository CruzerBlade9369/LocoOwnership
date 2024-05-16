using System;
using System.Reflection;

using UnityEngine;

using DV.ServicePenalty;
using DV.Simulation.Cars;
using DV.Utils;
using DV.ThingTypes;

namespace LocoOwnership.OwnershipHandler
{
	internal class DebtHandling
	{
		private SimulatedCarDebtTracker locoDebt;
		private SimulatedCarDebtTracker tenderDebt = null;

		public SimulatedCarDebtTracker DebtValueStealer(SimController simController)
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

		public bool RemoveTrackedLocoDebts(SimulatedCarDebtTracker locoDebt, SimulatedCarDebtTracker tenderDebt)
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
				throw new Exception($"The index is {num}! This shouldn't happen!");
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
					throw new Exception($"The tender index is {num2}! This shouldn't happen!");
				}
				existingTenderDebt = locoDebtController.trackedLocosDebts[num2];
				existingTenderDebt.UpdateDebtState();
				totalDebtCheck += existingTenderDebt.GetTotalPrice();
			}

			// If the (total) debt isn't 0 don't allow to buy loco
			Debug.Log(totalDebtCheck);
			if (totalDebtCheck > 0f)
			{
				return false;
			}

			// Remove car from tracked debts
			locoDebtController.trackedLocosDebts.RemoveAt(num);
			SingletonBehaviour<CareerManagerDebtController>.Instance.UnregisterDebt(existingLocoDebt);
			existingLocoDebt.UpdateDebtState();
			if (tenderDebt != null)
			{
				locoDebtController.trackedLocosDebts.RemoveAt(num2);
				SingletonBehaviour<CareerManagerDebtController>.Instance.UnregisterDebt(existingTenderDebt);
				existingTenderDebt.UpdateDebtState();
			}

			return true;
		}

		public bool SetVehicleToOwned(TrainCar car)
		{
			// Get car's sim controller component and steal debt component
			SimController simController = car.GetComponent<SimController>();
			locoDebt = DebtValueStealer(simController);

			// Check if S282 and get tender data too
			bool isSteamEngine = CarTypes.IsMUSteamLocomotive(car.carType);
			bool hasTender = car.rearCoupler.IsCoupled() && CarTypes.IsTender(car.rearCoupler.coupledTo.train.carLivery);

			TrainCar tender = null;
			SimController tenderSimController = null;

			if (isSteamEngine && hasTender)
			{
				tender = car.rearCoupler.coupledTo.train;
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
					Debug.Log("OnLogicCarInitialized method reinvoked on tender.");
				}
				car.uniqueCar = true;
				onLogicCarInitializedMethod.Invoke(simController, null);
				Debug.Log("OnLogicCarInitialized method reinvoked.");
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

		public bool RemoveExistingOwnedCarState(SimulatedCarDebtTracker locoDebt, SimulatedCarDebtTracker tenderDebt)
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
				throw new Exception($"The index is {num}! This shouldn't happen!");
			}
			ExistingOwnedCarDebt existingLocoDebt = ownedCarsStateController.existingOwnedCarStates[num];
			existingLocoDebt.UpdateDebtState();

			bool isLocoDebtOnlyEnv = existingLocoDebt.carDebtTrackerBase.IsDebtOnlyEnvironmental();
			totalDebtCheck += existingLocoDebt.GetTotalPrice();
			Debug.Log($"loco debt env only {isLocoDebtOnlyEnv}");
			Debug.Log(existingLocoDebt.GetTotalPrice());

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
					throw new Exception($"The tender index is {num2}! This shouldn't happen!");
				}
				existingTenderDebt = ownedCarsStateController.existingOwnedCarStates[num2];
				existingTenderDebt.UpdateDebtState();

				isTenderDebtOnlyEnv = existingTenderDebt.carDebtTrackerBase.IsDebtOnlyEnvironmental();
				totalDebtCheck += existingTenderDebt.GetTotalPrice();
				Debug.Log($"tender debt env only {isTenderDebtOnlyEnv}");
				Debug.Log(existingTenderDebt.GetTotalPrice());
			}

			// If the debts are only environmental then allow to sell loco
			if (existingTenderDebt != null)
			{
				if (!isTenderDebtOnlyEnv && !isLocoDebtOnlyEnv || totalDebtCheck > 0f)
				{
					return false;
				}
			}
			else
			{
				if (!isLocoDebtOnlyEnv || totalDebtCheck > 0f)
				{
					return false;
				}
			}

			ownedCarsStateController.existingOwnedCarStates.RemoveAt(num);
			existingLocoDebt.UpdateDebtState();
			if(tenderDebt != null)
			{
				ownedCarsStateController.existingOwnedCarStates.RemoveAt(num2);
				existingTenderDebt.UpdateDebtState();
			}
			
			return true;
		}

		public bool RemoveOwnedVehicle(TrainCar car)
		{
			// Get car's sim controller component and steal debt component
			SimController simController = car.GetComponent<SimController>();
			locoDebt = DebtValueStealer(simController);

			// Check if S282 and get tender data too
			bool isSteamEngine = CarTypes.IsMUSteamLocomotive(car.carType);
			bool hasTender = car.rearCoupler.IsCoupled() && CarTypes.IsTender(car.rearCoupler.coupledTo.train.carLivery);

			TrainCar tender = null;
			SimController tenderSimController = null;

			if (isSteamEngine && hasTender)
			{
				tender = car.rearCoupler.coupledTo.train;
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
					Debug.Log("OnLogicCarInitialized method reinvoked on tender.");
				}
				car.uniqueCar = false;
				onLogicCarInitializedMethod.Invoke(simController, null);
				Debug.Log("OnLogicCarInitialized method reinvoked.");
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
