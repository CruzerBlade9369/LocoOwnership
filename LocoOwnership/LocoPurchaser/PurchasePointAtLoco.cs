using System;

using DV;
using DV.Localization;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Shared;

namespace LocoOwnership.LocoPurchaser
{
	internal class PurchasePointAtLoco : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;

		private float carBuyPrice;
		private int trainCarMask;
		internal TrainCar selectedCar;
		private Transform signalOrigin;
		private CommsRadioCarDeleter carDeleter;
		private CarHighlighter highlighter;

		public PurchasePointAtLoco(TrainCar selectedCar, CommsRadioCarDeleter carDeleter, CarHighlighter highlighter)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/purchase"),
				contentText: LocalizationAPI.L("lo/radio/purchasing/content"),
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;
			this.carDeleter = carDeleter;
			this.highlighter = highlighter;

			signalOrigin = carDeleter.signalOrigin;

			if (this.selectedCar is null)
			{
				Main.DebugLog("selectedCar is null");
				throw new ArgumentNullException(nameof(selectedCar));
			}

			highlighter.InitHighlighter(selectedCar, carDeleter);
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				return this;
			}

			carBuyPrice = Finances.CalculateBuyPrice(selectedCar);

			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new TransactionPurchaseConfirm(selectedCar, carBuyPrice, carDeleter, highlighter, true);
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			RaycastHit hit;
			//if we're not pointing at anything
			if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out hit, SIGNAL_RANGE, trainCarMask))
			{
				return new PurchasePointAtNothing();
			}
			TrainCar target = TrainCar.Resolve(hit.transform.root);
			if (target is null || target != selectedCar)
			{
				//if we stopped pointing at selectedCar and are now pointing at either
				//nothing or another train car, then go back to PointingAtNothing so
				//we can figure out what we're pointing at
				return new PurchasePointAtNothing();
			}
			return this;
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			trainCarMask = highlighter.RefreshTrainCarMask();
			highlighter.StartHighlighter(utility, true);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			highlighter.StopHighlighter();
		}
	}
}
