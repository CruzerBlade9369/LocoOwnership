using System;
using System.Collections.Generic;
using System.Reflection;

using Newtonsoft.Json.Linq;

using UnityEngine;

using DV.JObjectExtstensions;
using DV.ServicePenalty;
using DV.Simulation.Cars;
using DV.Utils;

namespace LocoOwnership.Shared
{
	internal class OwnedLocos : MonoBehaviour/*, ISimulationFlowProvider*/
	{
		private const int MAX_OWNED_LOCOS = 16;

		private SimulatedCarDebtTracker? locoDebt;

		public class VehicleOwnershipResult
		{
			public bool MaxOwnedLoc { get; set; }
			public bool DebtNotZero { get; set; }
			public bool Success { get; set; }
		}

		// This is the cache
		public static Dictionary<string, string> ownedLocos = new Dictionary<string, string>();

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region CACHE HANDLER

		public static void ClearCache()
		{
			Main.DebugLog("Clearing owned loco list cache.");
			ownedLocos.Clear();
		}

		public VehicleOwnershipResult OnLocoBuy(TrainCar selectedCar)
		{
			var result = new VehicleOwnershipResult();

			// Check if player already has enough owned locos
			if (ownedLocos.Count >= MAX_OWNED_LOCOS)
			{
				result.MaxOwnedLoc = true;
				return result;
			}

			string guid = selectedCar.CarGUID;
			string locoID = $"{selectedCar.carType}";

			if (ownedLocos.ContainsKey(guid))
			{
				throw new Exception("Loco GUID duplicate!");
			}
			else
			{
				bool allowOwnVehicle = SetVehicleToOwned(selectedCar);
				if (!allowOwnVehicle)
				{
					result.DebtNotZero = true;
					return result;
				}

				ownedLocos.Add(guid, locoID);

				foreach (KeyValuePair<string, string> kvp in ownedLocos)
				{
					Main.DebugLog($"Key = {kvp.Key}, Value = {kvp.Value}");
				}

				result.Success = true;
				return result;
			}
		}

		public void OnLocoSell(TrainCar selectedCar)
		{
			string guid = selectedCar.CarGUID;

			if (ownedLocos.ContainsKey(guid))
			{
				ownedLocos.Remove(guid);
			}
			else
			{
				throw new Exception("Loco GUID not found!");
			}
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region VANILLA OWNED VEHICLES HANDLER

		public SimulatedCarDebtTracker DebtValueStealer(SimController simController)
		{
			// Use reflection to steal the private 'debt' field
			if (simController == null)
			{
				throw new Exception("SimController not found on the specified TrainCar!");
			}

			FieldInfo debtField = typeof(SimController).GetField("debt", BindingFlags.NonPublic | BindingFlags.Instance);
			if (debtField == null)
			{
				throw new Exception("Field 'debt' not found in SimController!");
			}

			locoDebt = (SimulatedCarDebtTracker)debtField.GetValue(simController);
			if (locoDebt == null)
			{
				throw new Exception("Value of locoDebt is null!");
			}

			return locoDebt;
		}

		public bool SetVehicleToOwned(TrainCar car)
		{
			car.uniqueCar = true;

			// Get car's sim controller component
			SimController simController = car.GetComponent<SimController>();

			// Steal debt component from car
			locoDebt = DebtValueStealer(simController);

			// Find the traincar in tracked loco debts to remove
			LocoDebtController locoDebtController = LocoDebtController.Instance;
			if (locoDebtController == null)
			{
				throw new Exception("LocoDebtController instance is null!");
			}

			int num = locoDebtController.trackedLocosDebts.FindIndex((ExistingLocoDebt debt) => debt.locoDebtTracker == locoDebt);
			if (num == -1)
			{
				throw new Exception($"The index is {num}! This shouldn't happen!");
			}
			ExistingLocoDebt existingLocoDebt = locoDebtController.trackedLocosDebts[num];

			Debug.Log(existingLocoDebt.GetTotalPrice());

			// If the debt isn't 0 don't allow to buy loco
			if (existingLocoDebt.GetTotalPrice() > 0f)
			{
				return false;
			}

			// Remove car from tracked debts
			locoDebtController.trackedLocosDebts.RemoveAt(num);
			SingletonBehaviour<CareerManagerDebtController>.Instance.UnregisterDebt(existingLocoDebt);
			existingLocoDebt.UpdateDebtState();

			// Invoke OnLogicCarInitialized with new uniqueCar value to register to owned vehicles list
			MethodInfo onLogicCarInitializedMethod = typeof(SimController).GetMethod("OnLogicCarInitialized", BindingFlags.NonPublic | BindingFlags.Instance);
			if (onLogicCarInitializedMethod != null)
			{
				onLogicCarInitializedMethod.Invoke(simController, null);
				Debug.Log("OnLogicCarInitialized method reinvoked.");
			}
			else
			{
				throw new Exception("onLogicCarInitialized method failed to be found!");
			}

			return true;
		}

		public void RemoveOwnedVehicle(TrainCar car)
		{

		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region LOAD/SAVE HANDLER

		// Convert JObject of owned locos back into dict and apply to cache
		public static void OnGameLoad(JObject savedOwnedLocos)
		{
			JObject[] jobjectArray = savedOwnedLocos.GetJObjectArray("savedOwnedLocos");

			if (jobjectArray != null)
			{
				foreach (JObject jobject in jobjectArray)
				{
					var guid = jobject.GetString("guid");
					var locoID = jobject.GetString("locoID");

					if (!ownedLocos.ContainsKey(guid))
					{
						ownedLocos.Add(guid, locoID);
					}
				}
			}
		}

		// Convert owned locos dict cache into JObjects for savegame
		public static JObject OnGameSaved()
		{
			JObject savedOwnedLocos = new();

			JObject[] array = new JObject[ownedLocos.Count];

			int i = 0;

			foreach (var kvp in ownedLocos)
			{
				JObject dataObject = new JObject();

				dataObject.SetString("guid", kvp.Key);
				dataObject.SetString("locoID", kvp.Value);

				array[i] = dataObject;

				i++;
			}

			savedOwnedLocos.SetJObjectArray("savedOwnedLocos", array);

			return savedOwnedLocos;
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/
	}
}
