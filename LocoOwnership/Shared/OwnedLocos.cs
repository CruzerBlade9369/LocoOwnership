using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using UnityEngine;

using DV;
using DV.JObjectExtstensions;
using DV.ServicePenalty;
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
	internal class OwnedLocos : MonoBehaviour, ISimulationFlowProvider
	{
		private const int MAX_OWNED_LOCOS = 16;

		public SimController simController = new();

		public SimConnectionDefinition connectionsDefinition;
		public ResourceContainerController resourceContainerController;
		public EnvironmentDamageController environmentDamageController;

		public SimulationFlow simFlow;
		public SimulationFlow SimulationFlow => simFlow;

		private SimulatedCarDebtTracker? scdt;

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
			string locoID = selectedCar.ID;

			if (ownedLocos.ContainsKey(guid))
			{
				throw new Exception("Loco GUID duplicate!");
			}
			else
			{
				resourceContainerController = simController.resourceContainerController;
				environmentDamageController = simController.environmentDamageController;
				connectionsDefinition = simController.connectionsDefinition;

				Main.DebugLog($"resourceContainerController {resourceContainerController}");
				Main.DebugLog($"environmentDamageController {environmentDamageController}");
				Main.DebugLog($"connectionsDefinition {connectionsDefinition}");

				simFlow = simController.simFlow;

				Main.DebugLog($"simFlow {simFlow}");

				DamageController component = selectedCar.GetComponent<DamageController>();

				Main.DebugLog($"component {component}");

				scdt = new SimulatedCarDebtTracker(component,
				resourceContainerController,
				environmentDamageController,
				simFlow,
				selectedCar.ID,
				selectedCar.carType);

				Main.DebugLog("register car");
				SingletonBehaviour<OwnedCarsStateController>.Instance.RegisterCarStateTracker(selectedCar, scdt);

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
