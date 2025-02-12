using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

using DV.Localization;
using DV.JObjectExtstensions;
using DV.ServicePenalty;
using DV.InventorySystem;

using UnityEngine;

using MessageBox;

using LocoOwnership.Shared;

namespace LocoOwnership.OwnershipHandler
{
	public class OwnedLocos : MonoBehaviour
	{
		public static OwnedLocos Instance { get; private set; }

		// This is the cache
		public static Dictionary<string, string> ownedLocos = new();
		public static Dictionary<string, float> ownedLocosLicensePrice = new();
		int len = ownedLocos.Count;

		// makes sure this thing stays as singleton
		// remind myself to not touch old code if this thing suddenly stops working.
		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(this.gameObject);
				return;
			}

			Instance = this;
			DontDestroyOnLoad(this.gameObject);
		}

		public static void Initialize()
		{
			if (Instance == null)
			{
				GameObject singletonObject = new GameObject(nameof(OwnedLocos));
				Instance = singletonObject.AddComponent<OwnedLocos>();
			}
		}

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region CACHE HANDLER

		public static void ClearCache()
		{
			Main.DebugLog("Clearing owned loco list cache.");
			ownedLocos.Clear();
			ownedLocosLicensePrice.Clear();
		}

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

			// Debug lines
			Main.DebugLog("Owned locos list:");
			foreach (KeyValuePair<string, string> kvp in ownedLocos)
			{
				Main.DebugLog($"Guid = {kvp.Key}, LocoID = {kvp.Value}");
			}

			// Add loco buy price for despawn refund
			ownedLocosLicensePrice.Add(guid, Finances.CalculateBuyPrice(selectedCar));

			// Debug lines
			Main.DebugLog("Owned locos list, stored loco price:");
			foreach (KeyValuePair<string, float> kvp in ownedLocosLicensePrice)
			{
				Main.DebugLog($"Guid = {kvp.Key}, stored loco price = {kvp.Value}");
			}
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
						Debug.Log($"Refunded purchase for despawned locomotive {id}, ${price}");
						ownedLocosLicensePrice.Remove(key);
					}

					ownedLocos.Remove(key);
					Debug.LogError($"Car {id} is detected to be destroyed! Removed from mod and vanilla owned cars list.");
				}
			}

			if (modOwnedCarsToRemove.Any())
			{
				PopupAPI.ShowOk(LocalizationAPI.L("lo/popupapi/okmsg/carvalidate"));
			}

			Main.DebugLog("Completed validation of existence of owned cars");
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
