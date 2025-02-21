using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

using DV.Localization;
using DV.JObjectExtstensions;
using DV.ServicePenalty;
using DV.InventorySystem;

using UnityEngine;

using LocoOwnership.Shared;

namespace LocoOwnership.OwnershipHandler
{
	public class OwnedLocos
	{
		// this is the cache
		private static Dictionary<string, string> ownedLocos = new();
		private static Dictionary<string, float> ownedLocosLicensePrice = new();

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region UTILITY

		public static bool HasLocoGUIDAsKey(string key)
		{
			if (ownedLocos.ContainsKey(key))
			{
				return true;
			}

			return false;
		}

		public static int CountLocosOnly()
		{
			return ownedLocos.Count(kv => kv.Key.StartsWith("L-"));
		}

		public static void ClearCache()
		{
			Main.DebugLog("Clearing owned loco list cache.");
			ownedLocos.Clear();
			ownedLocosLicensePrice.Clear();
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region OWNED LOCOS HANDLER

		public static void BuyLoco(TrainCar selectedCar)
		{
			string guid = selectedCar.CarGUID;
			string locoID = selectedCar.ID;

			TrainCar tender = CarGetters.GetTender(selectedCar);

			string tenderGuid = "";
			string tenderID = "";
			if (tender != null)
			{
				tenderGuid = tender.CarGUID;
				tenderID = tender.ID;
			}

			if (tender != null)
			{
				ownedLocos.Add(tenderGuid, tenderID);
			}
			ownedLocos.Add(guid, locoID);

			// add loco buy price for despawn refund
			ownedLocosLicensePrice.Add(guid, Finances.CalculateBuyPrice(selectedCar));

#if DEBUG

			// debug lines
			Main.DebugLog("Owned locos list:");
			foreach (KeyValuePair<string, string> kvp in ownedLocos)
			{
				Main.DebugLog($"Guid = {kvp.Key}, LocoID = {kvp.Value}");
			}

			Main.DebugLog("Owned locos list, stored loco price:");
			foreach (KeyValuePair<string, float> kvp in ownedLocosLicensePrice)
			{
				Main.DebugLog($"Guid = {kvp.Key}, stored loco price = {kvp.Value}");
			}

#endif

		}

		public static void SellLoco(TrainCar selectedCar)
		{
			string guid = selectedCar.CarGUID;

			TrainCar tender = CarGetters.GetTender(selectedCar);

			string tenderGuid = "";
			if (tender != null)
			{
				tenderGuid = tender.CarGUID;
			}

			if (tender != null)
			{
				ownedLocos.Remove(tenderGuid);
			}

			ownedLocos.Remove(guid);
			if (ownedLocosLicensePrice.ContainsKey(guid))
			{
				ownedLocosLicensePrice.Remove(guid);
			}
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region OWNED LOCOS VALIDATOR

		public static void OwnedCarsStatesValidate()
		{
			OwnedCarsStateController ocsc = OwnedCarsStateController.Instance;

			foreach (ExistingOwnedCarDebt eocd in ocsc.existingOwnedCarStates)
			{
				if (ownedLocos.ContainsKey(eocd.car.CarGUID))
				{
					if (!eocd.car.uniqueCar)
					{
						eocd.car.uniqueCar = true;
					}
				}
			}

			Main.DebugLog("Completed validation of owned cars states");
		}

		public static void ValidateOwnedCars()
		{
			OwnedCarsStateController ocsc = OwnedCarsStateController.Instance;
			HashSet<string> eocdGuids = new();
			HashSet<string> socdIDs = new();
			HashSet<StagedOwnedCarDebt> socdTemp = new();

			bool carsDeleted = false;

			if (ocsc == null)
			{
				return;
			}

			if (Inventory.Instance == null)
			{
				return;
			}

			// populate temporary list
			foreach (ExistingOwnedCarDebt eocd in ocsc.existingOwnedCarStates)
			{
				eocdGuids.Add(eocd.car.CarGUID);
			}

			foreach (StagedOwnedCarDebt socd in ocsc.currentlyDestroyedOwnedCarStates)
			{
				if (ownedLocos.Values.Contains(socd.ID))
				{
					Main.DebugLog($"Adding {socd.ID} into removal list since it exists in owned locos list.");
					socdTemp.Add(socd);
				}

				socdIDs.Add(socd.ID);
			}

			foreach (string guid in ownedLocos.Keys)
			{
				Debug.Log(ownedLocos[guid]);
			}

			// validate cars
			foreach (string guid in ownedLocos.Keys.ToList())
			{
				string carID = ownedLocos[guid];

				if (!eocdGuids.Contains(guid))
				{
					carsDeleted = true;

					Debug.LogError($"Car {carID} is gone! Refunding license cost and deleting from lists.");
					Inventory.Instance.AddMoney(ownedLocosLicensePrice[guid]);
					ownedLocos.Remove(guid);
				}
			}

			// remove from socd
			foreach (StagedOwnedCarDebt socd in socdTemp)
			{
				ocsc.currentlyDestroyedOwnedCarStates.Remove(socd);
			}

			if (carsDeleted)
			{
				CarDeletedNotif.ShowOK(LocalizationAPI.L("lo/popupapi/okmsg/carvalidate"));
			}

			Main.DebugLog("Completed validation of existence of owned cars");
		}

		/*public static void ValidateOwnedCarsOld()
		{
			OwnedCarsStateController ocsc = OwnedCarsStateController.Instance;
			List<StagedOwnedCarDebt> ownedCarsToDestage = new();
			List<string> modOwnedCarsToRemove = new();
			List<string> actualOwnedCars = new();

			// populate temporary lists
			foreach (StagedOwnedCarDebt socd in ocsc.currentlyDestroyedOwnedCarStates)
			{
				if (ownedLocos.ContainsValue(socd.ID))
				{
					ownedCarsToDestage.Add(socd);
					modOwnedCarsToRemove.Add(socd.ID);
				}
			}

			foreach (ExistingOwnedCarDebt eocd in ocsc.existingOwnedCarStates)
			{
				actualOwnedCars.Add(eocd.car.CarGUID);
			}

			// remove from destroyed owned car states
			foreach (StagedOwnedCarDebt socd in ownedCarsToDestage)
			{
				ocsc.currentlyDestroyedOwnedCarStates.Remove(socd);
			}

			// refund missing cars from mod owned locos list and remove
			foreach (string id in modOwnedCarsToRemove)
			{
				var keysToRemove = ownedLocos.Where(pair => pair.Value == id).Select(pair => pair.Key).ToList();

				foreach (var key in keysToRemove)
				{
					if (ownedLocosLicensePrice.ContainsKey(key))
					{
						ownedLocosLicensePrice.TryGetValue(key, out float price);
						Inventory.Instance.AddMoney(price);
						Debug.Log($"Refunded purchase for despawned locomotive {id}, ${price}");
						ownedLocosLicensePrice.Remove(key);
					}

					ownedLocos.Remove(key);
					Debug.LogError($"Car {id} is detected to be destroyed! Removed from mod and vanilla owned cars list.");
				}
			}

			// if there are data in owned locos list that don't exist in vanilla owned vehicles
			foreach (string guid in ownedLocos.Keys.ToList())
			{
				if (!actualOwnedCars.Contains(guid))
				{
					ownedLocos.TryGetValue(guid, out string value);
					Debug.LogError($"Car {value} does not exist in vanilla owned vehicles list! Removing from cache.");
					ownedLocos.Remove(guid);
					ownedLocosLicensePrice.Remove(guid);
				}
			}

			if (modOwnedCarsToRemove.Any())
			{
				CarDeletedNotif.ShowOK(LocalizationAPI.L("lo/popupapi/okmsg/carvalidate"));
			}

			Main.DebugLog("Completed validation of existence of owned cars");
		}*/

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region LOAD/SAVE HANDLER

		// convert JObject of owned locos back into dict and apply to cache
		public static void OnGameLoad(JObject savedOwnedLocos)
		{
			JObject[] jobjectArray = savedOwnedLocos.GetJObjectArray("savedOwnedLocos");
			JObject[] jobjectArrayPrice = savedOwnedLocos.GetJObjectArray("savedOwnedLocosLicensePrice");

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

			if (jobjectArrayPrice != null)
			{
				foreach (JObject jobject in jobjectArrayPrice)
				{
					var guidPrice = jobject.GetString("guidPrice");
					var licensePrice = jobject.GetFloat("licensePrice");

					if (!ownedLocosLicensePrice.ContainsKey(guidPrice))
					{
						ownedLocosLicensePrice.Add(guidPrice, (float)licensePrice);
					}
				}
			}
		}

		// convert owned locos dict cache into JObjects for savegame
		public static JObject OnGameSaved()
		{
			JObject savedOwnedLocos = new();

			JObject[] array = new JObject[ownedLocos.Count];
			JObject[] priceArray = new JObject[ownedLocosLicensePrice.Count];

			int i = 0;
			foreach (var kvp in ownedLocos)
			{
				JObject dataObject = new();

				dataObject.SetString("guid", kvp.Key);
				dataObject.SetString("locoID", kvp.Value);

				array[i] = dataObject;

				i++;
			}

			int j = 0;
			foreach (var kvp in ownedLocosLicensePrice)
			{
				JObject dataObject = new();

				dataObject.SetString("guidPrice", kvp.Key);
				dataObject.SetFloat("licensePrice", kvp.Value);

				priceArray[j] = dataObject;

				j++;
			}

			savedOwnedLocos.SetJObjectArray("savedOwnedLocos", array);
			savedOwnedLocos.SetJObjectArray("savedOwnedLocosLicensePrice", priceArray);

			return savedOwnedLocos;
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/
	}
}
