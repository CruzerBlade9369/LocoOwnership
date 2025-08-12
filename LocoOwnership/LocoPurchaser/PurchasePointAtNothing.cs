using DV;
using DV.Localization;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Menus;
using LocoOwnership.Shared;
using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.LocoPurchaser
{
	public class PurchasePointAtNothing : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 100f;

		public PurchasePointAtNothing()
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/purchase"),
				contentText: LocalizationAPI.L("lo/radio/purchasing/content"),
				actionText: LocalizationAPI.L("comms/cancel"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{ }

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				return this;
			}
			utility.PlaySound(VanillaSoundCommsRadio.Cancel);
			return new OwnershipMenus(0);
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			RaycastHit hit;
			if (!Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out hit, SIGNAL_RANGE, CarHighlighter.trainCarMask))
			{
				return this;
			}

			// try to get the car we're pointing at
			TrainCar selectedCar = TrainCar.Resolve(hit.transform.root);
			if (selectedCar == null)
			{
				return this;
			}

			// check if we're pointing at a locomotive
			bool isLoco = selectedCar.IsLoco;
			if (!isLoco)
			{
				return this;
			}

			// check if loco exists in owned locos cache
			if (OwnedLocosManager.HasLocoGUIDAsKey(selectedCar.CarGUID))
			{
				return this;
			}

			if (selectedCar.uniqueCar || selectedCar.playerSpawnedCar)
			{
				return this;
			}

			if (selectedCar.carLivery.requiredLicense == null)
			{
				return this;
			}

			utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
			return new PurchasePointAtLoco(selectedCar);
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour previous)
		{
			base.OnEnter(utility, previous);
		}
	}
}
