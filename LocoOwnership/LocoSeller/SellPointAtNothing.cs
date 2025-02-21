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

		private int trainCarMask;
		private Transform signalOrigin;
		private CommsRadioCarDeleter carDeleter;

		public SellPointAtNothing()
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L("lo/radio/selling/content"),
				actionText: LocalizationAPI.L("comms/cancel"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			RefreshRadioComponent();
		}

		private void RefreshRadioComponent()
		{
			trainCarMask = CarHighlighter.RefreshTrainCarMask();
			carDeleter = CarHighlighter.RefreshCarDeleterComponent();
			signalOrigin = carDeleter.signalOrigin;
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				return this;
			}
			utility.PlaySound(VanillaSoundCommsRadio.Cancel);
			return new LocoSell();
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			while (signalOrigin == null)
			{
				Main.DebugLog("signalOrigin is null for some reason");
				RefreshRadioComponent();
			}

			RaycastHit hit;
			if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out hit, SIGNAL_RANGE, trainCarMask))
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

			if (OwnedLocos.HasLocoGUIDAsKey(selectedCar.CarGUID))
			{
				utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
				return new SellPointAtLoco(selectedCar);
			}

			return this;
		}
	}
}
