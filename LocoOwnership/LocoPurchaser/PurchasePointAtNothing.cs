using System;

using DV;
using DV.Localization;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Menus;
using LocoOwnership.Shared;
using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.LocoPurchaser
{
	// this class detects what we're pointing at
	internal class PurchasePointAtNothing : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 100f;

		private int trainCarMask;
		private Transform signalOrigin;
		private CommsRadioCarDeleter carDeleter;
		private CarHighlighter highlighter;

		public PurchasePointAtNothing()
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/purchase"),
				contentText: LocalizationAPI.L("lo/radio/purchasing/content"),
				actionText: LocalizationAPI.L("comms/cancel"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			highlighter = new CarHighlighter();
			RefreshRadioComponent();
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				return this;
			}
			utility.PlaySound(VanillaSoundCommsRadio.Cancel);
			return new LocoPurchase();
		}

		private void RefreshRadioComponent()
		{
			trainCarMask = highlighter.RefreshTrainCarMask();
			carDeleter = highlighter.RefreshCarDeleterComponent();
			signalOrigin = carDeleter.signalOrigin;
		}

		// Detecting what we're looking at
		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			while (signalOrigin is null)
			{
				Main.DebugLog("signalOrigin is null for some reason");
				RefreshRadioComponent();
			}

			RaycastHit hit;

			// If we're not pointing at anything
			if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out hit, SIGNAL_RANGE, trainCarMask))
			{
				return this;
			}

			// Try to get the car we're pointing at
			TrainCar selectedCar = TrainCar.Resolve(hit.transform.root);

			// If we aren't pointing at a car
			if (selectedCar is null)
			{
				return this;
			}

			// If we're pointing at a locomotive
			bool isLoco = selectedCar.IsLoco;
			if (isLoco)
			{
				// Check if loco exists in owned locos cache
				if (OwnedLocos.ownedLocos.ContainsKey(selectedCar.CarGUID))
				{
					return this;
				}

				if (selectedCar.uniqueCar && selectedCar.playerSpawnedCar)
				{
					return this;
				}

				if (selectedCar.carLivery.requiredLicense is not null)
				{
					utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
					return new PurchasePointAtLoco(selectedCar, carDeleter, highlighter);
				}
			}
			else
			{
				return this;
			}

			return this;
		}
	}
}
