using System;
using System.Linq;
using DV;
using DV.Localization;
using UnityEngine;
using CommsRadioAPI;
using LocoOwnership.OwnershipHandler;
using LocoOwnership.Shared;
using DV.ThingTypes;

namespace LocoOwnership.LocoRequester
{
	public class RequestLocoSelector : AStateBehaviour
	{
		private static int selectedIndex = 0;
		private TrainCar selectedCar;

		public RequestLocoSelector() : base(
			new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/request"),
				contentText: OwnedLocosManager.Instance.GetLocoDisplayNameFromIndex(selectedIndex),
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			selectedCar = OwnedLocosManager.Instance.GetTrainCarFromIndex(selectedIndex);
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
					PreviousIndex();
					return new RequestLocoSelector();

				case InputAction.Down:
					NextIndex();
					return new RequestLocoSelector();

				default:
					Debug.LogError("Request loco selector: why are you here?");
					throw new Exception($"Unexpected action: {action}");
			}
		}

		private void NextIndex()
		{
			int nextIndex = selectedIndex + 1;

			if (nextIndex >= OwnedLocosManager.Instance.OwnedLocosTrackers.Count)
			{
				nextIndex = 0;
			}

			selectedIndex = nextIndex;
		}

		private void PreviousIndex()
		{
			int previousIndex = selectedIndex - 1;

			if (previousIndex < 0)
			{
				previousIndex = OwnedLocosManager.Instance.OwnedLocosTrackers.Count - 1;
			}

			selectedIndex = previousIndex;
		}

		public static void ValidateIndex()
		{
			var trackers = OwnedLocosManager.Instance.OwnedLocosTrackers;

			if (selectedIndex >= trackers.Count)
			{
				selectedIndex = trackers.Count - 1;
			}
		}
	}
}
