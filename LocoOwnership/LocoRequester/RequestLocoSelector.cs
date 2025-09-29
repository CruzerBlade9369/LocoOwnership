using System;
using System.Collections.Generic;
using System.Linq;
using DV;
using DV.Localization;
using DV.ServicePenalty;
using UnityEngine;
using CommsRadioAPI;
using LocoOwnership.OwnershipHandler;
using LocoOwnership.Shared;
using DV.ThingTypes;

namespace LocoOwnership.LocoRequester
{
	public class RequestLocoSelector : AStateBehaviour
	{
		private static Dictionary<string, string> requestableOwnedLocos = new();

		public static int selectedIndex;
		private TrainCar selectedCar;

		public RequestLocoSelector(int index = 0) : base(
			new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/request"),
				contentText: requestableOwnedLocos.Values.ElementAt(selectedIndex),
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			index = selectedIndex;
			selectedCar = TrainCarFromIndex(selectedIndex);
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			switch (action)
			{
				case InputAction.Activate:

					TrainCar tender = CarUtils.GetTender(selectedCar);

					if (CarTypes.IsMUSteamLocomotive(selectedCar.carType) && tender == null)
					{
						utility.PlaySound(VanillaSoundCommsRadio.Warning);
						return new RequestFail(2);
					}

					if (selectedCar.derailed)
					{
						utility.PlaySound(VanillaSoundCommsRadio.Warning);
						return new RequestFail(3);
					}

					if (CarTypes.IsMUSteamLocomotive(selectedCar.carType) && selectedCar.rearCoupler.coupledTo.train.derailed)
					{
						utility.PlaySound(VanillaSoundCommsRadio.Warning);
						return new RequestFail(3);
					}

					// Get car bounds
					Bounds? locoBounds = selectedCar?.Bounds;
					Bounds bounds = default(Bounds);
					if (tender != null)
					{
						Bounds? tenderBounds = tender.Bounds;

						bounds.Encapsulate(locoBounds.Value);
						bounds.Encapsulate(tenderBounds.Value);
						bounds.Expand(new Vector3(0f, 0f, 2f));
					}
					else
					{
						bounds = locoBounds.Value;
					}

					utility.PlaySound(VanillaSoundCommsRadio.Confirm);
					return new RequestDestinationPicker(selectedCar, bounds, utility.SignalOrigin);

				case InputAction.Up:
					return new RequestLocoSelector(PreviousIndex());

				case InputAction.Down:
					return new RequestLocoSelector(NextIndex());

				default:
					Debug.LogError("Request loco selector: why are you here?");
					throw new Exception($"Unexpected action: {action}");
			}
		}

		private int NextIndex()
		{
			int nextIndex = selectedIndex + 1;
			if (nextIndex >= requestableOwnedLocos.Count)
			{
				nextIndex = 0;
			}
			selectedIndex = nextIndex;
			return nextIndex;
		}

		private int PreviousIndex()
		{
			int previousIndex = selectedIndex - 1;
			if (previousIndex < 0)
			{
				previousIndex = requestableOwnedLocos.Count - 1;
			}
			selectedIndex = previousIndex;
			return previousIndex;
		}

		private TrainCar TrainCarFromIndex(int index)
		{
			OwnedCarsStateController ocsc = OwnedCarsStateController.Instance;
			string ownedLocoGuid = requestableOwnedLocos.Keys.ElementAt(index);

			TrainCar car = TrainCarRegistry.Instance.GetTrainCarByCarGuid(ownedLocoGuid);

			return car;
		}

		public static int GetRequestableLocosCount()
		{
			return requestableOwnedLocos.Count;
		}

		public static void RefreshRequestableLocos()
		{
			requestableOwnedLocos.Clear();
			var tempDict = new Dictionary<string, string>();

			OwnedCarsStateController ocsc = OwnedCarsStateController.Instance;

			foreach (ExistingOwnedCarDebt eocd in ocsc.existingOwnedCarStates)
			{
				if (eocd.car.carType == TrainCarType.Tender) continue;

				if (OwnedLocosManager.HasLocoGUIDAsKey(eocd.car.CarGUID))
				{
					tempDict.Add(eocd.car.CarGUID, $"{LocalizationAPI.L(eocd.car.carLivery.localizationKey)} {eocd.car.ID}");
				}
			}

			requestableOwnedLocos = tempDict.OrderBy(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		}
	}
}
