using System;
using System.Collections.Generic;

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
	internal class RequestLocoSelector : AStateBehaviour
	{
		public static List<string> requestableOwnedLocos = new List<string>();
		public static int lastIndex = 0;
		private int selectedIndex;

		internal TrainCar loco;

		public RequestLocoSelector(int selectedIndex) : base(
			new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/request"),
				contentText: $"{LocalizationAPI.L(TrainCarFromIndex(selectedIndex).carLivery.localizationKey)} {TrainCarFromIndex(selectedIndex).ID}",
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override
			)
		)
		{
			lastIndex = this.selectedIndex = selectedIndex;

			loco = TrainCarFromIndex(selectedIndex);
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			switch (action)
			{
				case InputAction.Activate:

					TrainCar tender = CarGetters.GetTender(loco);

					if (CarTypes.IsMUSteamLocomotive(loco.carType) && tender is null)
					{
						utility.PlaySound(VanillaSoundCommsRadio.Warning);
						return new RequestFail(2);
					}

					if (loco.derailed)
					{
						utility.PlaySound(VanillaSoundCommsRadio.Warning);
						return new RequestFail(3);
					}

					if (CarTypes.IsMUSteamLocomotive(loco.carType) && loco.rearCoupler.coupledTo.train.derailed)
					{
						utility.PlaySound(VanillaSoundCommsRadio.Warning);
						return new RequestFail(3);
					}

					Bounds? locoBounds = loco?.Bounds;
					Bounds bounds = default(Bounds);
					if (tender is not null)
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
					return new RequestDestinationPicker(loco, bounds, utility.SignalOrigin);

				case InputAction.Up:
					return new RequestLocoSelector(PreviousIndex());

				case InputAction.Down:
					return new RequestLocoSelector(NextIndex());

				default:
					Debug.Log("Request loco selector: why are you here?");
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
			return nextIndex;
		}

		private int PreviousIndex()
		{
			int previousIndex = selectedIndex - 1;
			if (previousIndex < 0)
			{
				previousIndex = requestableOwnedLocos.Count - 1;
			}
			return previousIndex;
		}

		public static void RefreshRequestableLocos()
		{
			requestableOwnedLocos.Clear();

			OwnedCarsStateController ocsc = OwnedCarsStateController.Instance;

			foreach (ExistingOwnedCarDebt eocd in ocsc.existingOwnedCarStates)
			{
				if (OwnedLocos.ownedLocos.ContainsValue(eocd.ID) && eocd.car.IsLoco)
				{
					requestableOwnedLocos.Add(eocd.car.CarGUID);
				}
			}
		}

		private static TrainCar TrainCarFromIndex(int index)
		{
			OwnedCarsStateController ocsc = OwnedCarsStateController.Instance;
			TrainCar loco = null;

			string ownedLocoGuid = requestableOwnedLocos[index];

			foreach (ExistingOwnedCarDebt eocd in ocsc.existingOwnedCarStates)
			{
				if (eocd.car.CarGUID == ownedLocoGuid && eocd.car.IsLoco)
				{
					loco = eocd.car;
					break;
				}
			}

			if (loco == null)
			{
				throw new Exception("content from index selector: traincar is null!!");
			}

			return loco;
		}
	}
}
