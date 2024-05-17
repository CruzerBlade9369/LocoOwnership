using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

using UnityEngine;

using MessageBox;
using DV.JObjectExtstensions;
using DV.ThingTypes;
using DV.ServicePenalty;
using DV.InventorySystem;
using DV.UserManagement;

using LocoOwnership.Shared;

namespace LocoOwnership.OwnershipHandler
{
	internal class OwnedLocos : MonoBehaviour
	{
		private const int MAX_OWNED_LOCOS = 16;

		DebtHandling debtHandling = new();

		public static Settings settings = new Settings();

		public class DebtHandlingResult
		{
			public bool MaxOwnedLoc { get; set; }
			public bool DebtNotZero { get; set; }
			public bool Success { get; set; }
		}

		// This is the cache
		public static Dictionary<string, string> ownedLocos = new();
		public static Dictionary<string, float> ownedLocosLicensePrice = new();

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
			ownedLocosLicensePrice.Clear();
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
			string locoID = selectedCar.ID;
			float locoLicensePrice = selectedCar.carLivery.requiredLicense.price;

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

				// Populate prices list for refund checking
				if (settings.freeOwnership)
				{
					ownedLocosLicensePrice.Add(guid, 0f);
				}

				if (settings.freeSandboxOwnership && UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
				{
					ownedLocosLicensePrice.Add(guid, 0f);
				}
				else
				{
					if (selectedCar.carType == TrainCarType.LocoShunter)
					{
						ownedLocosLicensePrice.Add(guid, Finances.DE2_ARTIFICIAL_LICENSE_PRICE * 2);
					}
					else
					{
						ownedLocosLicensePrice.Add(guid, locoLicensePrice * 2);
					}
				}
				
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
				if (ownedLocosLicensePrice.ContainsKey(guid))
				{
					ownedLocosLicensePrice.Remove(guid);
				}
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

		public static void ValidateOwnedCars()
		{
			OwnedCarsStateController ocsc = OwnedCarsStateController.Instance;
			List<StagedOwnedCarDebt> ownedCarsToDestage = new();
			List<string> modOwnedCarsToRemove = new();

			// Remove missing cars from vanilla owned locos list
			foreach (StagedOwnedCarDebt socd in ocsc.currentlyDestroyedOwnedCarStates)
			{
				if (ownedLocos.ContainsValue(socd.ID))
				{
					ownedCarsToDestage.Add(socd);
					modOwnedCarsToRemove.Add(socd.ID);
				}
			}

			foreach (StagedOwnedCarDebt socd in ownedCarsToDestage)
			{
				ocsc.currentlyDestroyedOwnedCarStates.Remove(socd);
			}

			// Refund missing cars from mod owned locos list and remove
			foreach (string id in modOwnedCarsToRemove)
			{
				var keysToRemove = ownedLocos.Where(pair => pair.Value == id).Select(pair => pair.Key).ToList();

				foreach (var key in keysToRemove)
				{
					if (ownedLocosLicensePrice.ContainsKey(key))
					{
						ownedLocosLicensePrice.TryGetValue(key, out float price);
						Inventory.Instance.AddMoney(price);
						Main.DebugLog($"Refunded purchase for {id}, ${price}");
						ownedLocosLicensePrice.Remove(key);
					}

					ownedLocos.Remove(key);
					Debug.LogError($"Car {id} is detected to be destroyed! Removed from mod and vanilla owned cars list.");
				}
			}

			if (modOwnedCarsToRemove.Any())
			{
				PopupAPI.ShowOk("One or more of your owned locomotives have despawned! These locomotives have " +
					"been removed from your owned vehicles list and you have been refunded accordingly.");
			}
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region LOAD/SAVE HANDLER

		// Convert JObject of owned locos back into dict and apply to cache
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

		// Convert owned locos dict cache into JObjects for savegame
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
