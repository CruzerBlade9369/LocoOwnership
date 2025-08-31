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

		public static void PrintAllOwnedLocos()
		{
			if (ownedLocos.Count <= 0 || ownedLocosLicensePrice.Count <= 0)
			{
				Debug.Log("You don't have owned locos yet or you haven't loaded into a save!");
			}
			else
			{
				Debug.Log("Owned locos list:");
				foreach (KeyValuePair<string, string> kvp in ownedLocos)
				{
					Debug.Log($"Guid = {kvp.Key}, LocoID = {kvp.Value}");
				}

				Debug.Log("Owned locos list, stored loco price:");
				foreach (KeyValuePair<string, float> kvp in ownedLocosLicensePrice)
				{
					Debug.Log($"Guid = {kvp.Key}, stored loco price = {kvp.Value}");
				}

				Debug.Log("-----");
				Debug.Log($"Found {ownedLocos.Count} vehicles, {CountLocosOnly()} being locos");
				Debug.Log($"Found {ownedLocosLicensePrice.Count} loco price data");
			}
		}

		public static bool HasLocoGUIDAsKey(string key)
		{
			if (ownedLocos.ContainsKey(key))
			{
				return true;
			}

			return false;
		}

		public static int CountLocosAsSets()
		{
			List<string> workingList = ownedLocos.Keys.ToList();
			int count = 0;

			while (workingList.Count > 0)
			{
				TrainCar car = OwnedCarsStateController.Instance.existingOwnedCarStates.Find(
					debt => debt.car.CarGUID == workingList[0]).car;

				List<TrainCar> trainSet = CarUtils.GetCCLTrainsetOrLocoAndTender(car);

				var setGuids = new HashSet<string>(trainSet.Select(tc => tc.CarGUID));
				workingList.RemoveAll(g => setGuids.Contains(g));

				count++;
			}

			return count;
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

		#region OWNED LOCOS HANDLER\

		private static void SetToOwned(TrainCar car)
		{
			var locoDebtController = LocoDebtController.Instance;
			var simController = car.GetComponent<SimController>();
			SimulatedCarDebtTracker locoDebt = simController.debt;
			List<ExistingLocoDebt> debts = locoDebtController.trackedLocosDebts;

			if (locoDebt == null)
			{
				Debug.LogWarning($"Debt for {car.ID} is missing, cannot continue own operation for this one");
				return;
			}

			ExistingLocoDebt locoDebtEntry = debts.Find(debt => debt.locoDebtTracker == locoDebt);

			if (locoDebtEntry != null)
			{
				Main.DebugLog($"Preparing unregister existing debt for {car.ID}");
				debts.Remove(locoDebtEntry);
				SingletonBehaviour<CareerManagerDebtController>.Instance.UnregisterDebt(locoDebtEntry);
				Main.DebugLog($"Successfully unregistered debt for {car.ID}");
				locoDebtEntry.UpdateDebtState();
			}
			else
			{
				Debug.LogWarning($"{car.ID} does not have existing loco debt, skipping removal");
			}

			Main.DebugLog($"Preparing to register {car.ID} as owned");
			car.uniqueCar = true;
			SingletonBehaviour<OwnedCarsStateController>.Instance.RegisterCarStateTracker(car, locoDebt);
			Main.DebugLog($"Registered {car.ID} as owned");
		}

		private static void UnsetOwned(TrainCar car)
		{
			var ownedCarsStateController = OwnedCarsStateController.Instance;
			var simController = car.GetComponent<SimController>();
			SimulatedCarDebtTracker locoDebt = simController.debt;
			List<ExistingOwnedCarDebt> debts = ownedCarsStateController.existingOwnedCarStates;

			if (locoDebt == null)
			{
				Debug.LogWarning($"Debt for {car.ID} is missing, cannot continue un-own operation for this one");
				return;
			}

			ExistingOwnedCarDebt locoDebtEntry = debts.Find(debt => debt.carDebtTrackerBase == locoDebt);

			if (locoDebtEntry != null)
			{
				Main.DebugLog("Removing loco from owned cars list");
				debts.Remove(locoDebtEntry);
				Main.DebugLog("Removed loco from owned cars list");
				locoDebtEntry.UpdateDebtState();
			}
			else
			{
				Debug.LogWarning($"{car.ID} does not have existing owned car debt, skipping removal");
			}

			// Register as regular DVRT loco
			Main.DebugLog($"Preparing to register {car.ID} as DVRT");
			car.uniqueCar = false;
			SingletonBehaviour<LocoDebtController>.Instance.RegisterLocoDebtTracker(car, locoDebt);
			Main.DebugLog($"Registered {car.ID} as DVRT");
		}

		public static void BuyLoco(TrainCar selectedCar)
		{
			List<TrainCar> trainSet = CarUtils.GetCCLTrainsetOrLocoAndTender(selectedCar);

			// process loco purchase
			foreach (TrainCar car in trainSet)
			{
				ownedLocos.Add(car.CarGUID, car.ID);
				ownedLocosLicensePrice.Add(car.CarGUID, PricesCalc.CalculateBuyPrice(car, getTotalTrainsetPrice: false));
				SetToOwned(car);
			}
		}

		public static void SellLoco(TrainCar selectedCar)
		{
			List<TrainCar> trainSet = CarUtils.GetCCLTrainsetOrLocoAndTender(selectedCar);

			// process loco sell
			foreach (TrainCar car in trainSet)
			{
				UnsetOwned(car);
				ownedLocos.Remove(car.CarGUID);
				ownedLocosLicensePrice.Remove(car.CarGUID);
			}
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
					// if existing owned car states dont have whats in ownedLocos
					if (!eocdGuids.Contains(guid))
					{
						carsDeleted = true;
						string carID = ownedLocos[guid];

						Debug.LogWarning($"Car {carID} (GUID: {guid}) no longer exists! Refunding purchase");

						if (ownedLocosLicensePrice.TryGetValue(guid, out var price))
						{
							Inventory.Instance.AddMoney(price);
						}
						else
						{
							Debug.LogError($"Error: no purchase price found for car {carID} (GUID: {guid})");
						}

						ownedLocos.Remove(guid);
						ownedLocosLicensePrice.Remove(guid);
					}
				}

				// orphaned data is stuff that does not have valid relations to any past existing locos
				// added this because back then i forgot to add something to remove purchase price entries
				// in selling logic

				// clean up orphaned license prices
				var orphanedPrices = ownedLocosLicensePrice.Keys
					.Where(guid => !ownedLocos.ContainsKey(guid))
					.ToList();

				foreach (var guid in orphanedPrices)
				{
					Debug.LogWarning($"Removing orphaned license price data for GUID: {guid}");
					ownedLocosLicensePrice.Remove(guid);
				}

				Debug.Log($"Owned cars validation complete");

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

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/


		#region LOAD/SAVE HANDLER V2



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
