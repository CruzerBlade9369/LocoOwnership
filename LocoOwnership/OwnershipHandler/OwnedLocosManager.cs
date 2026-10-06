using DV.JObjectExtstensions;
using DV.Localization;
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
		[SerializeField] private List<LocoOwnershipController> _ownedLocosTrackers = new();
		public List<LocoOwnershipController> OwnedLocosTrackers => _ownedLocosTrackers;

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
			ValidateOwnedCars();

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

				var loc = car.gameObject.AddComponent<LocoOwnershipController>();
				loc.Initialize(PricesCalc.CalculateBuyPrice(car, getTotalTrainsetPrice: false));

				_ownedLocosTrackers.Add(loc);
			}
		}

		public void UnsetLocoFromOwned(TrainCar selectedCar)
		{
			List<TrainCar> trainSet = CarUtils.GetCCLTrainsetOrLocoAndTender(selectedCar);

			foreach (TrainCar car in trainSet)
			{
				var loc = car.gameObject.GetComponent<LocoOwnershipController>();
				if (loc != null)
				{
					loc.RemoveOwnership();
				}
				else
				{
					Debug.LogError($"{car.ID} doesn't have the ownership tracker component for some reason.");
				}

				_ownedLocosTrackers.Remove(loc);
			}
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region OWNED LOCOS VALIDATOR V2

		public void ValidateOwnedCars()
		{
			Debug.Log("Beginning validating existence of owned cars");
			List<string> invalidGuids = new();
			int locosToAssignComponents = 0;

			if (ownedLocosGuidsAndValuesTemp.Count > 0 && _ownedLocosTrackers.Count == 0)
			{
				// Assign tracker to valid locos and validate only if there are temp values and no trackers

				foreach (var guid in ownedLocosGuidsAndValuesTemp.Keys)
				{
					TrainCar loco = TrainCarRegistry.Instance?.GetTrainCarByCarGuid(guid);
					if (loco != null)
					{
						Main.DebugLog($"Adding tracker to {loco.ID}");
						var loc = loco.gameObject.AddComponent<LocoOwnershipController>();
						loc.Initialize(ownedLocosGuidsAndValuesTemp[guid]);

						_ownedLocosTrackers.Add(loc);

						locosToAssignComponents++;
					}
					else
					{
						Debug.LogWarning($"Car with GUID {guid} is gone!");
						invalidGuids.Add(guid);
						continue;
					}
				}
			}

			// Clear stale and invalid data in tracker and temp
			_ownedLocosTrackers.RemoveAll(x => x == null);
			if (invalidGuids.Count > 0)
			{
				foreach (var guid in invalidGuids)
				{
					ownedLocosGuidsAndValuesTemp.Remove(guid);
				}

				CoroutineHelper.StartCoro(ShowDelayedPopup());
			}

			// TODO: Remove uniquecar and handle debts
			// don't forget loco requesting

			Debug.Log($"Validated {_ownedLocosTrackers} locos, removed {invalidGuids.Count} locos, assigned ownership component to {locosToAssignComponents} locos");
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
