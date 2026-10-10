using DV.InventorySystem;
using DV.JObjectExtstensions;
using DV.Localization;
using DV.ServicePenalty;
using DV.Utils;
using LocoOwnership.Shared;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LocoOwnership.OwnershipHandler
{
	public class OwnedLocosManager : SingletonBehaviour<OwnedLocosManager>
	{
		// This list is the main source of truth after validation is complete
		[SerializeField] private List<LocoOwnershipController> _ownedLocosTrackers = new();
		public List<LocoOwnershipController> OwnedLocosTrackers => _ownedLocosTrackers;

		// This dict is the source of truth before validation, as validation is where assignment of trackers happen
		// This should not be referenced at any point after first validation is performed
		private Dictionary<string, float> ownedLocosGuidsAndValuesTemp = new();

		public new static string AllowAutoCreate()
		{
			return "[OwnedLocosManager]";
		}

		private void OnEnable()
		{
			WorldStreamingInit.LoadingFinished += ValidateOwnedCars;
		}

		private void OnDisable()
		{
			WorldStreamingInit.LoadingFinished -= ValidateOwnedCars;
		}

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region UTILITY

		public void PrintAllOwnedLocos()
		{
			if (_ownedLocosTrackers.Count <= 0)
			{
				Debug.Log("You don't have owned locos yet or you haven't loaded into a save!");
			}
			else
			{
				Debug.Log("Owned locos list:");
				for (int i = 0; i < _ownedLocosTrackers.Count; i++)
				{
					LocoOwnershipController tracker = _ownedLocosTrackers[i];
					var car = tracker.Car;

					if (car == null || tracker == null) continue;

					string id = car.ID;
					float purchaseValue = tracker.GetUnitPurchaseValue();

					Debug.Log($"{i}. Guid = {tracker.CarGUID}, LocoID = {id}, purchase price = {purchaseValue}");
				}

				Debug.Log("-----");
				Debug.Log($"Found {_ownedLocosTrackers.Count} vehicles, {CountIndividualLocoUnits()} being loco units");
			}
		}

		public bool IsLocoAlreadyOwned(string carGuid)
		{
			return _ownedLocosTrackers.Any(l => l != null && l.CarGUID == carGuid);
		}

		public int CountLocosAsSets()
		{
			return _ownedLocosTrackers
				.Where(l => l != null && l.Car != null)
				.Where(l => l.Car.ID.StartsWith("L-"))
				.Select(l =>
				{
					var car = TrainCarRegistry.Instance.GetTrainCarByCarGuid(l.CarGUID);
					var set = CarUtils.GetCCLTrainsetOrLocoAndTender(car);
					return new HashSet<string>(set.Select(tc => tc.CarGUID));
				})
				.Distinct(HashSet<string>.CreateSetComparer())
				.Count();

			// thanks Zeibach for helping with this part!
		}

		public int CountIndividualLocoUnits()
		{
			return _ownedLocosTrackers.Where(l => l != null && l.Car != null).Count(l => l.Car.ID.StartsWith("L-"));
		}

		public LocoOwnershipController GetOwnershipComponentFromGuid(string guid)
		{
			return _ownedLocosTrackers.FirstOrDefault(l => l.CarGUID == guid);
		}

		public string GetLocoDisplayName(int index)
		{
			var tracker = _ownedLocosTrackers
				.Where(l => l != null && l.Car != null)
				.ElementAt(index);

			return $"{LocalizationAPI.L(tracker.Car.carLivery.localizationKey)} {tracker.Car.ID}";
		}

		public void ClearTracker()
		{
			Main.DebugLog("Clearing owned loco list tracker and temp cache.");
			_ownedLocosTrackers.Clear();
			ownedLocosGuidsAndValuesTemp.Clear();
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region OWNED LOCOS HANDLER V2

		public void SetLocoToOwned(TrainCar selectedCar)
		{
			List<TrainCar> trainSet = CarUtils.GetCCLTrainsetOrLocoAndTender(selectedCar);

			foreach (TrainCar car in trainSet)
			{
				var tracker = car.gameObject.AddComponent<LocoOwnershipController>();
				tracker.Initialize(PricesCalc.CalculateBuyPrice(car, getTotalTrainsetPrice: false));

				_ownedLocosTrackers.Add(tracker);

				// Make sure car is no longer considered as player spawned
				car.playerSpawnedCar = false;

				DebtHandling.RegisterDebtToWorkTrain(car);
			}
		}

		public void UnsetLocoFromOwned(TrainCar selectedCar)
		{
			List<TrainCar> trainSet = CarUtils.GetCCLTrainsetOrLocoAndTender(selectedCar);

			foreach (TrainCar car in trainSet)
			{
				var tracker = car.gameObject.GetComponent<LocoOwnershipController>();
				if (tracker != null)
				{
					tracker.RemoveOwnership();
				}
				else
				{
					Debug.LogError($"{car.ID} doesn't have the ownership tracker component for some reason.");
				}

				_ownedLocosTrackers.Remove(tracker);

				DebtHandling.RegisterDebtToDVRT(car);
			}
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region OWNED LOCOS VALIDATOR V2

		// To be called from patch
		public void AssignOwnershipComponent(TrainCar car)
		{
			if (!ownedLocosGuidsAndValuesTemp.Keys.Contains(car.CarGUID))
			{
				Main.DebugLog($"Skipping ownership component of {car.ID}");
				return;
			}

			Main.DebugLog($"Assigning ownership component to {car.ID}");

			var tracker = car.gameObject.AddComponent<LocoOwnershipController>();
			tracker.Initialize(ownedLocosGuidsAndValuesTemp[car.CarGUID]);

			_ownedLocosTrackers.Add(tracker);
		}

		public void ValidateOwnedCars()
		{
			Debug.Log("Beginning validating existence of owned cars");
			List<string> invalidGuids = new();
			int validatedLocomotives = 0;

			// Validate if temp values and tracker don't match
			// Execute right after loading finished before player has the chance to modify the tracker
			if (ownedLocosGuidsAndValuesTemp.Count > 0 && _ownedLocosTrackers.Count > 0)
			{
				foreach (var guid in ownedLocosGuidsAndValuesTemp.Keys)
				{
					var tracker = _ownedLocosTrackers.FirstOrDefault(t => t.CarGUID == guid);
					if (tracker != null)
					{
						// Make sure cars are not unique
						tracker.Car.uniqueCar = false;

						validatedLocomotives++;
					}
					else
					{
						Debug.LogWarning($"Car with GUID {guid} is not found!");
						invalidGuids.Add(guid);
						continue;
					}
				}
			}

			// Clear stale and invalid data in tracker and temp, and refund player
			_ownedLocosTrackers.RemoveAll(x => x == null);
			if (invalidGuids.Count > 0)
			{
				foreach (var guid in invalidGuids)
				{
					Inventory.Instance.AddMoney(ownedLocosGuidsAndValuesTemp[guid]);
					ownedLocosGuidsAndValuesTemp.Remove(guid);
				}

				CoroutineHelper.StartCoro(ShowDelayedPopup());
			}

			// TODO: Remove uniquecar and handle debts
			// don't forget loco requesting

			Debug.Log($"Validated {_ownedLocosTrackers.Count} loco entries, removed {invalidGuids.Count} locos, {validatedLocomotives} locos are valid");
		}

		private IEnumerator ShowDelayedPopup()
		{
			yield return new WaitForSeconds(1f);
			CarDeletedNotif.ShowOK(LocalizationAPI.L("lo/popupapi/okmsg/carvalidate"));
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region LOAD/SAVE HANDLER V2

		public void OnGameLoad(JObject savedOwnedLocos)
		{
			JObject[] ownedLocosTrackerObject = savedOwnedLocos.GetJObjectArray("ownedLocosTrackerObject");

			if (ownedLocosTrackerObject == null)
			{
				// Migrate if v2 does not yet exist
				Main.DebugLog("v2 not found, beginning migration");

				JObject[] ownedLocosJobject = savedOwnedLocos.GetJObjectArray("savedOwnedLocos");
				JObject[] ownedLocosPricesJobject = savedOwnedLocos.GetJObjectArray("savedOwnedLocosLicensePrice");

				// v1 dataforms, migration only runs if v1 also exists
				Dictionary<string, string> ownedLocosTemp = new();
				Dictionary<string, float> ownedLocosLicensePriceTemp = new();

				if (ownedLocosJobject != null)
				{
					foreach (JObject jobject in ownedLocosJobject)
					{
						var guid = jobject.GetString("guid");
						var locoID = jobject.GetString("locoID");

						if (!ownedLocosTemp.ContainsKey(guid))
						{
							ownedLocosTemp.Add(guid, locoID);
						}
					}
				}

				if (ownedLocosPricesJobject != null)
				{
					foreach (JObject jobject in ownedLocosPricesJobject)
					{
						var guidPrice = jobject.GetString("guidPrice");
						var licensePrice = jobject.GetFloat("licensePrice");

						if (!ownedLocosLicensePriceTemp.ContainsKey(guidPrice))
						{
							ownedLocosLicensePriceTemp.Add(guidPrice, (float)licensePrice);
						}
					}
				}

				Main.DebugLog($"Found {ownedLocosTemp.Count} items in savedOwnedLocos, {ownedLocosLicensePriceTemp.Count} in license price");
				Main.DebugLog($"Migrating now...");

				// Migrator to v2
				foreach (var kvp in ownedLocosTemp)
				{
					ownedLocosGuidsAndValuesTemp.Add(kvp.Key, ownedLocosLicensePriceTemp[kvp.Key]);
				}

				Main.DebugLog($"Successfully migrated {ownedLocosGuidsAndValuesTemp.Count} locos to v2");
			}
			else
			{
				// If v2 exists, do not run migration and only load as normal
				Main.DebugLog("v2 found, skipping migration");

				foreach (JObject jobject in ownedLocosTrackerObject)
				{
					var locoGuid = jobject.GetString("locoGuid");
					var purchaseValue = (float)jobject.GetFloat("purchaseValue");

					ownedLocosGuidsAndValuesTemp.Add(locoGuid, purchaseValue);
				}
			}
		}

		// Convert owned locos trackers into JObjects for savegame
		public JObject OnGameSaved()
		{
			JObject savedOwnedLocos = new();
			List<JObject> trackerData = new();

			foreach (var tracker in _ownedLocosTrackers)
			{
				JObject dataObject = new();
				dataObject.SetString("locoGuid", tracker.CarGUID);
				dataObject.SetFloat("purchaseValue", tracker.GetUnitPurchaseValue());

				trackerData.Add(dataObject);
			}

			savedOwnedLocos.SetJObjectArray("ownedLocosTrackerObject", trackerData.ToArray());

			return savedOwnedLocos;
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/
	}
}
