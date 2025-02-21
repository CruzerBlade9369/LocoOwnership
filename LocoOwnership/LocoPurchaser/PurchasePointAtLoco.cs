using System;

using DV;
using DV.Localization;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Shared;

namespace LocoOwnership.LocoPurchaser
{
	public class PurchasePointAtLoco : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;

		private TrainCar selectedCar;

		private int trainCarMask;
		private Transform signalOrigin;
		private CommsRadioCarDeleter carDeleter;
		private CarHighlighter highlighter;

		public PurchasePointAtLoco(TrainCar selectedCar)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/purchase"),
				contentText: LocalizationAPI.L("lo/radio/purchasing/content"),
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;

			if (highlighter == null )
			{
				highlighter = new CarHighlighter();
			}

			if (this.selectedCar == null)
			{
				Main.DebugLog("selectedCar is null");
				throw new ArgumentNullException(nameof(selectedCar));
			}

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

			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new TransactionPurchaseConfirm(selectedCar, true);
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			RaycastHit hit;
			if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out hit, SIGNAL_RANGE, trainCarMask))
			{
				return new PurchasePointAtNothing();
			}

			TrainCar target = TrainCar.Resolve(hit.transform.root);
			if (target == null || target != selectedCar)
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
			highlighter.InitHighlighter(selectedCar, carDeleter);
			highlighter.StartHighlighter(utility, true);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			highlighter.StopHighlighter();
		}
	}
}
