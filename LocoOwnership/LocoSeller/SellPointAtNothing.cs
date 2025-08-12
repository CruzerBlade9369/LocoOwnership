using DV;
using DV.Localization;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Menus;
using LocoOwnership.Shared;
using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.LocoSeller
{
	public class SellPointAtNothing : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 100f;

		public SellPointAtNothing()
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L("lo/radio/selling/content"),
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
			return new OwnershipMenus(1);
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

			// check if the car we're pointing at is valid to sell
			if (!selectedCar.IsLoco)
			{
				return this;
			}

			if (selectedCar.carLivery.requiredLicense == null)
			{
				return this;
			}

			if (OwnedLocosManager.HasLocoGUIDAsKey(selectedCar.CarGUID))
			{
				utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
				return new SellPointAtLoco(selectedCar);
			}

			return this;
		}
	}
}
