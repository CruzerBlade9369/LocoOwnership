using System;

using DV;

using UnityEngine;

using CommsRadioAPI;
using LocoOwnership.Menus;


namespace LocoOwnership.LocoPurchaser
{
	// this class detects what we're pointing at
	internal class PurchasePointAtNothing : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 100f;

		private Transform signalOrigin;
		private int trainCarMask;

		private string carID;
		private float carBuyPrice;

		public PurchasePointAtNothing()
			: base(new CommsRadioState(
				titleText: "Purchase",
				contentText: "Aim at the locomotive you wish to purchase.",
				actionText: "Cancel",
				buttonBehaviour: ButtonBehaviourType.Override))
		{

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
			trainCarMask = LayerMask.GetMask(new string[]
			{
			"Train_Big_Collider"
			});
			ICommsRadioMode? commsRadioMode = ControllerAPI.GetVanillaMode(VanillaMode.Clear);
			if (commsRadioMode is null)
			{
				Main.DebugLog("Could not find CommsRadioCarDeleter");
				throw new NullReferenceException();
			}
			CommsRadioCarDeleter carDeleter = (CommsRadioCarDeleter)commsRadioMode;
			signalOrigin = carDeleter.signalOrigin;
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
			carID = selectedCar.ID;
			carBuyPrice = ((selectedCar.carLivery.requiredLicense.price)*2f);

			// If we aren't pointing at a car
			if (selectedCar is null)
			{
				return this;
			}

			// If we're pointing at a locomotive
			bool isLoco = selectedCar.IsLoco;
			if (isLoco)
			{
				utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
				return new PurchasePointAtLoco(selectedCar, carID, carBuyPrice);
			}
			else
			{
				return this;
			}

			// Keeping this here just in case
			Main.DebugLog("PurchasePointAtNothing OnUpdate: You shouldn't be here");
			return this;
		}
	}
}
