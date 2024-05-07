using System;

using DV;

using UnityEngine;

using CommsRadioAPI;
using LocoOwnership.Shared;

namespace LocoOwnership.LocoPurchaser
{
	internal abstract class TransactionPurchaseCancelState : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;

		internal TrainCar selectedCar;
		private Transform signalOrigin;
		private int trainCarMask;

		private string carID;
		private float carBuyPrice;

		private CarHighlighter highlighter;

		public TransactionPurchaseCancelState(TrainCar selectedCar, string carID, float carBuyPrice)
			: base(new CommsRadioState(
				titleText: "Purchase",
				contentText: $"Purchase {carID} for ${carBuyPrice}?",
				actionText: "Cancel",
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;
			this.carID = carID;
			this.carBuyPrice = carBuyPrice;

			if (this.selectedCar is null)
			{
				Main.DebugLog("selectedCar is null");
				throw new ArgumentNullException(nameof(selectedCar));
			}

			// Steal some components from vanilla modes
			ICommsRadioMode? commsRadioMode = ControllerAPI.GetVanillaMode(VanillaMode.Clear);
			if (commsRadioMode is null)
			{
				Main.DebugLog("Could not find CommsRadioCarDeleter");
				throw new NullReferenceException();
			}

			CommsRadioCarDeleter carDeleter = (CommsRadioCarDeleter)commsRadioMode;

			highlighter = new CarHighlighter();

			signalOrigin = carDeleter.signalOrigin;
			highlighter.InitHighlighter(selectedCar, carDeleter);
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
				return new TransactionPurchaseConfirm(selectedCar, carID, carBuyPrice);
			}
			else
			{
				return this;
			}
			// Keeping this here just in case
			Main.DebugLog("TransactionPurchaseCancelState OnUpdate: You shouldn't be here");
			return this;
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			trainCarMask = LayerMask.GetMask(new string[]
			{
			"Train_Big_Collider"
			});
			highlighter.StartHighlighter(utility, previous, false);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			highlighter.StopHighlighter(utility, next);
		}
	}
}
