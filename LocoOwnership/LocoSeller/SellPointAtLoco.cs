using System;
using DV;
using DV.Localization;
using UnityEngine;
using CommsRadioAPI;
using LocoOwnership.Shared;

namespace LocoOwnership.LocoSeller
{
	public class SellPointAtLoco : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;
		private TrainCar selectedCar;

		public SellPointAtLoco(TrainCar selectedCar)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L("lo/radio/selling/content"),
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;
			if (this.selectedCar == null)
			{
				throw new ArgumentNullException(nameof(selectedCar));
			}
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				return this;
			}

			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new TransactionSellConfirm(selectedCar, true);
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			RaycastHit hit;
			if (!Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out hit, SIGNAL_RANGE, CarHighlighter.trainCarMask))
			{
				return new SellPointAtNothing();
			}

			TrainCar target = TrainCar.Resolve(hit.transform.root);
			if (target == null || target != selectedCar)
			{
				//if we stopped pointing at selectedCar and are now pointing at either
				//nothing or another train car, then go back to PointingAtNothing so
				//we can figure out what we're pointing at
				return new SellPointAtNothing();
			}
			return this;
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			CarHighlighter.StartSelectorHighlighter(utility, selectedCar);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			CarHighlighter.StopSelectorHighlighter();
		}
	}
}
