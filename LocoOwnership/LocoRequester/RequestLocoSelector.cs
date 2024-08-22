using System;
using System.Collections.Generic;

using UnityEngine;

using DV;
using DV.Localization;
using DV.ServicePenalty;
using DV.ThingTypes;

using CommsRadioAPI;

using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.LocoRequester
{
	internal class RequestLocoSelector : AStateBehaviour
	{
		public static List<string> requestableOwnedLocos = new List<string>();
		public static int lastIndex = 0;
		private int selectedIndex;

		public RequestLocoSelector(int selectedIndex) : base(
			new CommsRadioState(
				titleText:"request",
				contentText: ContentFromIndex(selectedIndex),
				actionText:"confirm",
				buttonBehaviour: ButtonBehaviourType.Override
			)
		)
		{
			lastIndex = this.selectedIndex = selectedIndex;
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			switch (action)
			{
				case InputAction.Activate:
					TrainCarLivery selectedCarLivery = null;

					OwnedCarsStateController ocsc = OwnedCarsStateController.Instance;
					foreach (ExistingOwnedCarDebt eocd in ocsc.existingOwnedCarStates)
					{
						if (eocd.car.CarGUID == requestableOwnedLocos[selectedIndex] && eocd.car.IsLoco)
						{
							selectedCarLivery = eocd.car.carLivery;
						}
					}

					if (selectedCarLivery == null)
					{
						throw new Exception("LocoRequest: selected car livery is null!!");
					}

					GameObject? prefab = selectedCarLivery.prefab;
					TrainCar? trainCar = prefab?.GetComponent<TrainCar>();
					Bounds? carBounds = trainCar?.Bounds;
					utility.PlaySound(VanillaSoundCommsRadio.Confirm);
					return new RequestDestinationPicker(selectedIndex, carBounds.Value, utility.SignalOrigin);

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

		private static string ContentFromIndex(int index)
		{
			OwnedCarsStateController ocsc = OwnedCarsStateController.Instance;
			TrainCar loco = null;

			string ownedLocoGuid = requestableOwnedLocos[index];

			foreach (ExistingOwnedCarDebt eocd in ocsc.existingOwnedCarStates)
			{
				if (eocd.car.CarGUID == ownedLocoGuid && eocd.car.IsLoco)
				{
					loco = eocd.car;
				}
			}

			if (loco == null)
			{
				Debug.LogError("content from index selector: traincar is null!!");
				return "ERROR: THE TRAINCAR IS NULL";
			}

			string name = LocalizationAPI.L(loco.carLivery.localizationKey);
			return $"{name} {loco.ID}";
		}
	}
}
