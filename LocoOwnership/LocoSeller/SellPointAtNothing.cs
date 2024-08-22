using System;

using DV;
using DV.Localization;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Menus;
using LocoOwnership.Shared;
using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.LocoSeller
{
	// this class detects what we're pointing at
	internal class SellPointAtNothing : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 100f;

		private Transform signalOrigin;
		private int trainCarMask;

		private Finances finances;
		private CarHighlighter highlighter;

		private string carID;
		private float carSellPrice;

		public SellPointAtNothing()
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L("lo/radio/selling/content"),
				actionText: LocalizationAPI.L("lo/radio/general/cancel"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			finances = new Finances();
			highlighter = new CarHighlighter();
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			// Steal some components from other radio modes
			refreshSignalOriginAndTrainCarMask();
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}
			utility.PlaySound(VanillaSoundCommsRadio.Cancel);
			return new LocoSell();
		}

		private void refreshSignalOriginAndTrainCarMask()
		{
			trainCarMask = highlighter.RefreshTrainCarMask();
			signalOrigin = highlighter.RefreshSignalOrigin();
		}

		// Detecting what we're looking at
		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			while (signalOrigin is null)
			{
				Main.DebugLog("signalOrigin is null for some reason");
				refreshSignalOriginAndTrainCarMask();
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

			// Check if the car we're pointing at exists in owned locos cache
			if (selectedCar.IsLoco)
			{
				if (OwnedLocos.ownedLocos.ContainsKey(selectedCar.CarGUID))
				{
					if (selectedCar.carLivery.requiredLicense is not null)
					{
						// Get car information before passing down to PointAtLoco
						carID = selectedCar.ID;
						carSellPrice = finances.CalculateSellPrice(selectedCar);

						utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
						return new SellPointAtLoco(selectedCar, carID, carSellPrice);
					}
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
