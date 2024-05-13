using System;

using DV;

using UnityEngine;

using CommsRadioAPI;
using LocoOwnership.Shared;

namespace LocoOwnership.LocoPurchaser
{
	internal abstract class TransactionPurchaseConfirmState : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;

		internal TrainCar selectedCar;
		private Transform signalOrigin;
		private CommsRadioCarDeleter carDeleter;
		private int trainCarMask;

		private string carID;
		private float carBuyPrice;

		private CarHighlighter highlighter;

		public TransactionPurchaseConfirmState(TrainCar selectedCar, string carID, float carBuyPrice)
			: base(new CommsRadioState(
				titleText: "Purchase",
				contentText: $"Purchase {carID} for ${carBuyPrice}?",
				actionText: "Confirm",
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

			highlighter = new CarHighlighter();

			carDeleter = highlighter.RefreshCarDeleterComponent();
			signalOrigin = carDeleter.signalOrigin;
			highlighter.InitHighlighter(selectedCar, carDeleter);
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			RaycastHit hit;
			// If we're not pointing at anything
			if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out hit, SIGNAL_RANGE, trainCarMask))
			{
				return new TransactionPurchaseCancel(selectedCar, carID, carBuyPrice);
			}

			TrainCar target = TrainCar.Resolve(hit.transform.root);

			if (target is null || target != selectedCar)
			{
				return new TransactionPurchaseCancel(selectedCar, carID, carBuyPrice);
			}
			return this;
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			trainCarMask = highlighter.RefreshTrainCarMask();
			highlighter.StartHighlighter(utility, previous, true);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			highlighter.StopHighlighter(utility, next);
		}
	}
}
