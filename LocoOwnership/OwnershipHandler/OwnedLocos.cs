using System;
using System.Collections.Generic;

using MessageBox;

using Newtonsoft.Json.Linq;

using UnityEngine;

using DV.JObjectExtstensions;
using DV.Utils;
using DV.ThingTypes;

namespace LocoOwnership.OwnershipHandler
{
	internal class OwnedLocos : MonoBehaviour
	{
		private const int MAX_OWNED_LOCOS = 16;

		DebtHandling debtHandling = new();

		public class DebtHandlingResult
		{
			public bool MaxOwnedLoc { get; set; }
			public bool DebtNotZero { get; set; }
			public bool Success { get; set; }
		}

		// This is the cache
		public static Dictionary<string, string> ownedLocos = new Dictionary<string, string>();

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region CACHE HANDLER

		public static TrainCar GetTender(TrainCar selectedCar)
		{
			// Check if we're buying S282
			bool isSteamEngine = CarTypes.IsMUSteamLocomotive(selectedCar.carType);
			bool hasTender = selectedCar.rearCoupler.IsCoupled() && CarTypes.IsTender(selectedCar.rearCoupler.coupledTo.train.carLivery);

			TrainCar tender = null;

			// Get tender if S282
			if (isSteamEngine && hasTender)
			{
				tender = selectedCar.rearCoupler.coupledTo.train;
			}

			return tender;
		}

		public static void ClearCache()
		{
			Main.DebugLog("Clearing owned loco list cache.");
			ownedLocos.Clear();
		}

		public DebtHandlingResult OnLocoBuy(TrainCar selectedCar)
		{
			var result = new DebtHandlingResult();

			// Check if player already has enough owned locos
			if (ownedLocos.Count >= MAX_OWNED_LOCOS)
			{
				result.MaxOwnedLoc = true;
				return result;
			}

			string guid = selectedCar.CarGUID;
			string locoID = $"{selectedCar.carType}";

			TrainCar tender = GetTender(selectedCar);

			string tenderGuid = "";
			string tenderID = "";
			if (tender != null)
			{
				tenderGuid = tender.CarGUID;
				tenderID = tender.ID;
			}

			if (ownedLocos.ContainsKey(guid))
			{
				throw new Exception("Loco GUID duplicate!");
			}
			else
			{
				bool allowOwnVehicle = debtHandling.SetVehicleToOwned(selectedCar, tender);
				if (!allowOwnVehicle)
				{
					result.DebtNotZero = true;
					return result;
				}

				if (tender != null)
				{
					ownedLocos.Add(tenderGuid, tenderID);
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

		public DebtHandlingResult OnLocoSell(TrainCar selectedCar)
		{
			var result = new DebtHandlingResult();

			string guid = selectedCar.CarGUID;

			TrainCar tender = GetTender(selectedCar);

			string tenderGuid = "";
			if (tender != null)
			{
				tenderGuid = tender.CarGUID;
			}

			if (ownedLocos.ContainsKey(guid))
			{
				bool allowSellVehicle = debtHandling.RemoveOwnedVehicle(selectedCar, tender);
				if (!allowSellVehicle)
				{
					result.DebtNotZero = true;
					return result;
				}

				if (tender != null)
				{
					ownedLocos.Remove(tenderGuid);
				}

				ownedLocos.Remove(guid);
			}
			else
			{
				throw new Exception("Loco GUID not found!");
			}

			result.Success = true;
			return result;
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region OWNED LOCOS VALIDATOR

		public void ValidateOwnedCars()
		{
			HashSet<string> allLocoGuids = new HashSet<string>();
			List<string> missingCars = new List<string>();

			foreach (TrainCar car in SingletonBehaviour<CarSpawner>.Instance.AllLocos)
			{
				allLocoGuids.Add(car.CarGUID);
			}

			foreach (string carGuid in ownedLocos.Keys)
			{
				if (!allLocoGuids.Contains(carGuid))
				{
					missingCars.Add(carGuid);
				}
			}

			foreach (string missingCarGuid in missingCars)
			{
				Debug.Log($"Car with GUID {missingCarGuid} does not exist in the world, removing.");
				// Optionally, remove from ownedLocos if that is desired
				ownedLocos.Remove(missingCarGuid);
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
