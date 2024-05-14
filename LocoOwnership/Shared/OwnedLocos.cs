using System;
using System.Collections.Generic;
using System.Reflection;

using Newtonsoft.Json.Linq;

using UnityEngine;

using DV;
using DV.JObjectExtstensions;
using DV.ServicePenalty;
using DV.ServicePenalty.UI;
using DV.Simulation.Cars;
using DV.Utils;
using DV.Logic.Job;
using DV.PitStops;
using DV.Damage;
using DV.Simulation.Controllers;
using LocoSim.Implementations;
using LocoSim.Definitions;

namespace LocoOwnership.Shared
{
	internal class OwnedLocos : MonoBehaviour/*, ISimulationFlowProvider*/
	{
		private const int MAX_OWNED_LOCOS = 16;
		/*public SimController simController = new();

		public SimConnectionDefinition? connectionsDefinition;
		public ResourceContainerController? resourceContainerController;
		public EnvironmentDamageController? environmentDamageController;

		public SimulationFlow? simFlow;
		public SimulationFlow? SimulationFlow => simFlow;

		private CareerManagerDebtController cmdc = new();
		private OwnedCarsStateController ocsc = new();

		public static SimulatedCarDebtTracker Debt { get; set; }*/

		// This is the cache
		public static Dictionary<string, string> ownedLocos = new Dictionary<string, string>();

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region CACHE HANDLER

		public static void ClearCache()
		{
			Main.DebugLog("Clearing owned loco list cache.");
			ownedLocos.Clear();
		}

		public bool OnLocoBuy(TrainCar selectedCar)
		{
			bool cachingSuccess = false;

			// Check if player already has enough owned locos
			if (ownedLocos.Count >= MAX_OWNED_LOCOS)
			{
				return cachingSuccess;
			}

			string guid = selectedCar.CarGUID;
			string locoID = $"{selectedCar.carType}";

			if (ownedLocos.ContainsKey(guid))
			{
				throw new Exception("Loco GUID duplicate!");
			}
			else
			{
				/*// Reinitialize SimController and set uniqueCar to true to utilize existing vanilla owned vehicles feature
				DamageController component = selectedCar.GetComponent<DamageController>();
				SimController newSimController = selectedCar.GetComponent<SimController>();

				if (newSimController != null)
				{
					selectedCar.uniqueCar = true;
					newSimController.Initialize(selectedCar, component);
				}
				else
				{
					throw new Exception("SimController is null!");
				}*/

				selectedCar.uniqueCar = true;

				Main.DebugLog($"uniquecar: {selectedCar.uniqueCar}");
				Debug.Log($"{selectedCar.carType}");
				
				ownedLocos.Add(guid, locoID);

				foreach (KeyValuePair<string, string> kvp in ownedLocos)
				{
					Main.DebugLog($"Key = {kvp.Key}, Value = {kvp.Value}");
				}

				cachingSuccess = true;
				return cachingSuccess;
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

		/*public void ReinitDebtTracker(SimController simController)
		{
			MethodInfo onLogicCarInitializedMethod = typeof(SimController).GetMethod("OnLogicCarInitialized",
				BindingFlags.NonPublic | BindingFlags.Instance);

			if (onLogicCarInitializedMethod != null)
			{
				// Invoke the method on the instance of SimController
				onLogicCarInitializedMethod.Invoke(simController, null);
			}
			else
			{
				Debug.LogError("OnLogicCarInitialized method not found!");
			}
		}

		public void SetVehicleToOwned(TrainCar car)
		{
			SimulatedCarDebtTracker debt = Debt;

			if (debt == null)
			{
				throw new Exception("SCDT debt is null!");
			}

			SingletonBehaviour<OwnedCarsStateController>.Instance.RegisterCarStateTracker(car, debt);
		}

		public void RemoveOwnedVehicle(TrainCar car)
		{

		}*/

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
