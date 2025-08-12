using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json.Linq;

using DV.Localization;
using DV.JObjectExtstensions;
using DV.ServicePenalty;
using DV.InventorySystem;
using DV.Simulation.Cars;
using DV.Utils;

using UnityEngine;

using LocoOwnership.Shared;
using System.Collections;

namespace LocoOwnership.OwnershipHandler
{
	public class OwnedLocosManager
	{
		// this is the cache
		private static Dictionary<string, string> ownedLocos = new();
		private static Dictionary<string, float> ownedLocosLicensePrice = new();

		public static Dictionary<string, string> OwnedLocos => ownedLocos;
		public static Dictionary<string, float> OwnedLocosLicensePrice => ownedLocosLicensePrice;

		public static void Initialize()
		{
			//WorldStreamingInit.LoadingFinished += OwnedCarsStatesValidate;
			WorldStreamingInit.LoadingFinished += ValidateOwnedCars;
		}

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
			return ownedLocos.Count(kv => kv.Value.StartsWith("L-"));
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

		private static void SetToOwned(TrainCar car, TrainCar tender = null)
		{
			var locoDebtController = LocoDebtController.Instance;
			var simController = car.GetComponent<SimController>();
			var locoDebt = simController.debt;

			SimController tenderSimController = null;
			SimulatedCarDebtTracker tenderDebt = null;
			if (tender != null)
			{
				tenderSimController = tender.GetComponent<SimController>();
				tenderDebt = tenderSimController.debt;
			}

			// find indices and entries is one loop
			int locoIndex = -1, tenderIndex = -1;
			ExistingLocoDebt locoDebtEntry = null, tenderDebtEntry = null;
			var debts = locoDebtController.trackedLocosDebts;
			for (int i = 0; i < debts.Count && (locoIndex == -1 || (tenderDebt != null && tenderIndex == -1)); i++)
			{
				if (debts[i].locoDebtTracker == locoDebt)
				{
					locoIndex = i;
					locoDebtEntry = debts[i];
				}
				else if (tenderDebt != null && debts[i].locoDebtTracker == tenderDebt)
				{
					tenderIndex = i;
					tenderDebtEntry = debts[i];
				}
			}

			if (locoIndex == -1)
			{
				Debug.LogError("SetToOwned: loco debt not found!");
				return;
			}
			if (tenderDebt != null && tenderIndex == -1)
			{
				Debug.LogError($"SetToOwned: tender debt not found!");
				return;
			}

			// remove tender debt first if present
			if (tenderDebtEntry != null)
			{
				Main.DebugLog("Prepare unregister tender debt");
				debts.RemoveAt(tenderIndex);
				SingletonBehaviour<CareerManagerDebtController>.Instance.UnregisterDebt(tenderDebtEntry);
				Main.DebugLog("Unregistered tender debt");
				tenderDebtEntry.UpdateDebtState();

				// adjust loco index if tender was before loco
				if (tenderIndex < locoIndex)
					locoIndex--;
			}

			Main.DebugLog("Prepare unregister loco debt");
			debts.RemoveAt(locoIndex);
			SingletonBehaviour<CareerManagerDebtController>.Instance.UnregisterDebt(locoDebtEntry);
			Main.DebugLog("Unregistered loco debt");
			locoDebtEntry.UpdateDebtState();

			if (tenderSimController != null)
			{
				tender.uniqueCar = true;
				SingletonBehaviour<OwnedCarsStateController>.Instance.RegisterCarStateTracker(tender, tenderDebt);
				Main.DebugLog("Registered tender debt as owned");
			}
			car.uniqueCar = true;
			SingletonBehaviour<OwnedCarsStateController>.Instance.RegisterCarStateTracker(car, locoDebt);
			Main.DebugLog("Registered loco debt as owned");
		}

		private static void UnsetOwned(TrainCar car, TrainCar tender)
		{
			var ownedCarsStateController = OwnedCarsStateController.Instance;
			var locoDebtController = LocoDebtController.Instance;
			var simController = car.GetComponent<SimController>();
			var locoDebt = simController.debt;

			SimController tenderSimController = null;
			SimulatedCarDebtTracker tenderDebt = null;
			if (tender != null)
			{
				tenderSimController = tender.GetComponent<SimController>();
				tenderDebt = tenderSimController.debt;
			}

			// Find both indices and entries in one loop
			int locoIndex = -1, tenderIndex = -1;
			ExistingOwnedCarDebt locoDebtEntry = null, tenderDebtEntry = null;
			var ownedStates = ownedCarsStateController.existingOwnedCarStates;
			for (int i = 0; i < ownedStates.Count && (locoIndex == -1 || (tenderDebt != null && tenderIndex == -1)); i++)
			{
				if (ownedStates[i].carDebtTrackerBase == locoDebt)
				{
					locoIndex = i;
					locoDebtEntry = ownedStates[i];
				}
				else if (tenderDebt != null && ownedStates[i].carDebtTrackerBase == tenderDebt)
				{
					tenderIndex = i;
					tenderDebtEntry = ownedStates[i];
				}
			}

			if (locoIndex == -1)
			{
				Debug.LogError($"UnsetOwned: loco debt not found!");
				return;
			}
			if (tenderDebt != null && tenderIndex == -1)
			{
				Debug.LogError($"UnsetOwned: tender debt not found!");
				return;
			}

			// Remove tender first if present
			if (tenderDebtEntry != null)
			{
				Main.DebugLog("Removing tender from owned cars list");
				tenderDebtEntry.car.uniqueCar = false;
				ownedStates.RemoveAt(tenderIndex);
				Main.DebugLog("Removed tender from owned cars list");
				tenderDebtEntry.UpdateDebtState();

				// adjust loco index if tender was before loco
				if (tenderIndex < locoIndex)
					locoIndex--;
			}

			Main.DebugLog("Removing loco from owned cars list");
			locoDebtEntry.car.uniqueCar = false;
			ownedStates.RemoveAt(locoIndex);
			Main.DebugLog("Removed loco from owned cars list");
			locoDebtEntry.UpdateDebtState();

			// Register as DVRT
			if (tenderSimController != null)
			{
				tender.uniqueCar = false;
				SingletonBehaviour<LocoDebtController>.Instance.RegisterLocoDebtTracker(tender, tenderDebt);
				Main.DebugLog("Registered tender debt as DVRT");
			}
			car.uniqueCar = false;
			SingletonBehaviour<LocoDebtController>.Instance.RegisterLocoDebtTracker(car, locoDebt);
			Main.DebugLog("Registered loco debt as DVRT");
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

			// add to owned locos list
			if (tender != null)
			{
				ownedLocos.Add(tenderGuid, tenderID);
			}
			ownedLocos.Add(guid, locoID);

			// add loco buy price for despawn refund
			ownedLocosLicensePrice.Add(guid, PricesCalc.CalculateBuyPrice(selectedCar));

			// mark vehicle as owned
			SetToOwned(selectedCar, tender);

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

			// remove from owned locos list
			if (tender != null)
			{
				ownedLocos.Remove(tenderGuid);
			}
			ownedLocos.Remove(guid);

			// remove loco price
			if (ownedLocosLicensePrice.ContainsKey(guid))
			{
				ownedLocosLicensePrice.Remove(guid);
			}

			// mark vehicle back to DVRT
			UnsetOwned(selectedCar, tender);
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region OWNED LOCOS VALIDATOR

		public static void ValidateOwnedCars()
		{
			Debug.Log("Beginning validating existence of owned cars");

			try
			{
				if (OwnedCarsStateController.Instance == null)
				{
					Debug.LogError("Owned cars state controller is null while trying to validate owned cars");
					return;
				}

				if (Inventory.Instance == null)
				{
					Debug.LogError("Inventory instance is null while trying to validate owned cars");
					return;
				}

				var ocsc = OwnedCarsStateController.Instance;
				bool carsDeleted = false;

				// build temporary lists
				var eocdGuids = new HashSet<string>(
					ocsc.existingOwnedCarStates
						.Where(eocd => eocd?.car != null)
						.Select(eocd => eocd.car.CarGUID)
				);

				// process staged deletions
				var carsToStagedDelete = ocsc.currentlyDestroyedOwnedCarStates
					.Where(socd => socd != null && ownedLocos.Values.Contains(socd.ID))
					.ToList();

				foreach (var socd in carsToStagedDelete)
				{
					Main.DebugLog($"Removing {socd.ID} from staged debt list");
					ocsc.currentlyDestroyedOwnedCarStates.Remove(socd);
				}

				// validate owned cars against existing state
				foreach (var guid in ownedLocos.Keys.ToList())
				{
					if (!eocdGuids.Contains(guid))
					{
						carsDeleted = true;
						string carID = ownedLocos[guid];

						Debug.LogWarning($"Car {carID} (GUID: {guid}) no longer exists! Refunding license");

						if (ownedLocosLicensePrice.TryGetValue(guid, out var price))
						{
							Inventory.Instance.AddMoney(price);
						}
						else
						{
							Debug.LogError($"Error: no license price found for car {carID} (GUID: {guid})");
						}

						ownedLocos.Remove(guid);
						ownedLocosLicensePrice.Remove(guid);
					}
				}

				// orphaned data is stuff that exists in the LO lists but not in the ingame list
				// different from the previous checks where those have the associated staged cars data
				// but here there is no associated staged data

				// clean up orphaned loco data
				var orphanedLocos = ownedLocos.Keys
					.Where(guid => !eocdGuids.Contains(guid))
					.ToList();

				foreach (var guid in orphanedLocos)
				{
					Debug.LogWarning($"Removing orphaned loco data: {guid}, {ownedLocos[guid]}");
					ownedLocos.Remove(guid);
				}

				// clean up orphaned license prices
				var orphanedPrices = ownedLocosLicensePrice.Keys
					.Where(guid => !ownedLocos.ContainsKey(guid))
					.ToList();

				foreach (var guid in orphanedPrices)
				{
					Debug.LogWarning($"Removing orphaned license price data for GUID: {guid}");
					ownedLocosLicensePrice.Remove(guid);
				}

				Debug.Log($"Owned cars validation completed. Removed {carsToStagedDelete.Count} disappeared locomotives and {orphanedPrices.Count} orphaned prices");

				if (carsDeleted)
				{
					CoroutineHelper.StartCoro(ShowDelayedPopup());
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"[ValidateOwnedCars] Unexpected error: {ex.Message}");
				Debug.LogException(ex);
			}
		}

		private static IEnumerator ShowDelayedPopup()
		{
			yield return new WaitForSeconds(1f);
			CarDeletedNotif.ShowOK(LocalizationAPI.L("lo/popupapi/okmsg/carvalidate"));
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
