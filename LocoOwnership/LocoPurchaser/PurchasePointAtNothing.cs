using CommsRadioAPI;
using DV;
using DV.Localization;
using LocoOwnership.Menus;
using LocoOwnership.OwnershipHandler;
using LocoOwnership.Shared;
using UnityEngine;

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
			return new OwnershipMenus();
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			RaycastHit hit;
			if (!Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out hit, SIGNAL_RANGE, CarHighlighter.trainCarMask))
			{
				return this;
			}

			// Try to get the car we're pointing at
			TrainCar selectedCar = TrainCar.Resolve(hit.transform.root);
			if (selectedCar == null)
			{
				return this;
			}

			// Check if we're pointing at a locomotive
			if (!selectedCar.IsLoco)
			{
				return this;
			}

			// Succeeded by the next block
			/*if (OwnedLocosManager.Instance.IsLocoGuidAlreadyOwned(selectedCar.CarGUID))
			{
				return this;
			}*/

			// Skip if car is owned
			if (selectedCar.TryGetComponent<LocoOwnershipController>(out _))
			{
				return this;
			}

			// Avoid uniquecar
			if (selectedCar.uniqueCar)
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
