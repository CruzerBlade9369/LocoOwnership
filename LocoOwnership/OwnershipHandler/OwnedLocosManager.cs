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
		[SerializeField] private List<string> _ownedLocosGuidsTracker = new();
		public List<string> OwnedLocosGuidsTracker => _ownedLocosGuidsTracker;

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
			if (_ownedLocosGuidsTracker.Count <= 0)
			{
				Debug.Log("You don't have owned locos yet or you haven't loaded into a save!");
			}
			else
			{
				Debug.Log("Owned locos list:");
				for (int i = 0; i < _ownedLocosGuidsTracker.Count; i++)
				{
					string guid = _ownedLocosGuidsTracker[i];

					var car = TrainCarRegistry.Instance.GetTrainCarByCarGuid(guid);
					var ownership = GetOwnershipComponentFromGuid(guid);
					if (car == null || ownership == null) continue;

					string id = car.ID;
					float purchaseValue = ownership.GetUnitPurchaseValue();

					Debug.Log($"{i}. Guid = {guid}, LocoID = {id}, purchase price = {purchaseValue}");
				}

				Debug.Log("-----");
				Debug.Log($"Found {_ownedLocosGuidsTracker.Count} vehicles, {CountIndividualLocoUnits()} being loco units");
			}
		}

		public bool IsLocoGuidAlreadyOwned(string guid)
		{
			if (_ownedLocosGuidsTracker.Contains(guid))
			{
				return true;
			}

			return false;
		}

		public int CountLocosAsSets()
		{
			ValidateOwnedCars();

			return _ownedLocosGuidsTracker
				.Where(str => str.StartsWith("L-"))
				.Select(str =>
				{
					var car = TrainCarRegistry.Instance.GetTrainCarByCarGuid(str);
					var set = CarUtils.GetCCLTrainsetOrLocoAndTender(car);
					return new HashSet<string>(set.Select(tc => tc.CarGUID));
				})
				.Distinct(HashSet<string>.CreateSetComparer())
				.Count();

			// thanks Zeibach for helping with this part!
		}

		public int CountIndividualLocoUnits()
		{
			return _ownedLocosGuidsTracker.Count(str => str.StartsWith("L-"));
		}

		public LocoOwnershipController GetOwnershipComponentFromGuid(string guid)
		{
			TrainCar loco = TrainCarRegistry.Instance.GetTrainCarByCarGuid(guid);
			if (loco == null)
			{
				return null;
			}
			return loco.gameObject.GetComponent<LocoOwnershipController>();
		}

		public void ClearCache()
		{
			Main.DebugLog("Clearing owned loco list cache.");
			_ownedLocosGuidsTracker.Clear();
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

				_ownedLocosGuidsTracker.Add(car.CarGUID);
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

				_ownedLocosGuidsTracker.Remove(car.CarGUID);
			}
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region OWNED LOCOS VALIDATOR V2

		public void ValidateOwnedCars()
		{
			Debug.Log("Beginning validating existence of owned cars");
			List<string> invalidGuids = new List<string>();

			bool carsGone = false;

			foreach (var guid in _ownedLocosGuidsTracker)
			{
				TrainCar loco = TrainCarRegistry.Instance?.GetTrainCarByCarGuid(guid);
				if (loco == null)
				{
					Debug.LogWarning($"Car with GUID {guid} is gone!");
					invalidGuids.Add(guid);
					continue;
				}

				var ownership = loco.gameObject.GetComponent<LocoOwnershipController>();
				if (ownership == null)
				{
					Debug.LogWarning($"Car {guid} missing ownership component!");
					invalidGuids.Add(guid);
				}
			}

			if (invalidGuids.Count > 0)
			{
				foreach (var guid in invalidGuids)
				{
					_ownedLocosGuidsTracker.Remove(guid);
				}

				CoroutineHelper.StartCoro(ShowDelayedPopup());
			}
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
				// this block runs if v2 doesn't exist
				JObject[] ownedLocosJobject = savedOwnedLocos.GetJObjectArray("savedOwnedLocos");
				JObject[] ownedLocosPricesJobject = savedOwnedLocos.GetJObjectArray("savedOwnedLocosLicensePrice");
				
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

				// migrator to v2
				foreach (var kvp in ownedLocosTemp)
				{
					TrainCar loco = TrainCarRegistry.Instance.GetTrainCarByCarGuid(kvp.Key);
					if (loco != null)
					{
						if (!loco.gameObject.TryGetComponent<LocoOwnershipController>(out var l))
						{
							var loc = loco.gameObject.AddComponent<LocoOwnershipController>();

							if (ownedLocosLicensePriceTemp.TryGetValue(kvp.Key, out float price))
							{
								loc.Initialize(price);
							}
							else
							{
								loc.Initialize(0f);
							}

							_ownedLocosGuidsTracker.Add(kvp.Key);
						}
					}
				}
			}
			else
			{
				// normal v2 loader
				foreach (JObject jobject in ownedLocosTrackerObject)
				{
					var locoGuid = jobject.GetString("locoGuid");
					var purchaseValue = (float)jobject.GetFloat("purchaseValue");

					TrainCar loco = TrainCarRegistry.Instance.GetTrainCarByCarGuid(locoGuid);
					if (loco != null)
					{
						if (!loco.gameObject.TryGetComponent<LocoOwnershipController>(out var l))
						{
							var loc = loco.gameObject.AddComponent<LocoOwnershipController>();
							loc.Initialize(purchaseValue);
						}

						_ownedLocosGuidsTracker.Add(locoGuid);
					}
				}
			}
		}

		// convert owned locos dict cache into JObjects for savegame
		public JObject OnGameSaved()
		{
			JObject savedOwnedLocos = new();
			List<JObject> trackerData = new();

			int k = 0;
			foreach (var guid in _ownedLocosGuidsTracker)
			{
				var ownership = GetOwnershipComponentFromGuid(guid);
				if (ownership == null) continue;

				JObject dataObject = new();
				dataObject.SetString("locoGuid", guid);
				dataObject.SetFloat("purchaseValue", ownership.GetUnitPurchaseValue());

				trackerData.Add(dataObject);
			}

			savedOwnedLocos.SetJObjectArray("ownedLocosTrackerObject", trackerData.ToArray());

			return savedOwnedLocos;
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/
	}
}
