using System;

using DV;

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

		private Transform signalOrigin;
		private int trainCarMask;

		private Finances finances;
		private CarHighlighter highlighter;

		private string carID;
		private float carBuyPrice;

		public PurchasePointAtNothing()
			: base(new CommsRadioState(
				titleText: "Purchase",
				contentText: "Aim at the locomotive you wish to purchase.",
				actionText: "Cancel",
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
			return new LocoPurchase();
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

			// If we're pointing at a locomotive
			bool isLoco = selectedCar.IsLoco;
			if (isLoco)
			{
				// Check if loco exists in owned locos cache
				if (OwnedLocos.ownedLocos.ContainsKey(selectedCar.CarGUID))
				{
					return this;
				}

				if (selectedCar.carLivery.requiredLicense is not null)
				{
					// Get car information before passing down to PointAtLoco
					carID = selectedCar.ID;
					carBuyPrice = finances.CalculateBuyPrice(selectedCar);

					utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
					return new PurchasePointAtLoco(selectedCar, carID, carBuyPrice);
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
