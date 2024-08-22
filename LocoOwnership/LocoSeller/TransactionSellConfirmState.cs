using System;

using DV;
using DV.Localization;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Shared;

namespace LocoOwnership.LocoSeller
{
	internal abstract class TransactionSellConfirmState : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;

		internal TrainCar selectedCar;
		private Transform signalOrigin;
		private CommsRadioCarDeleter carDeleter;
		private int trainCarMask;

		private string carID;
		private float carSellPrice;

		private CarHighlighter highlighter;

		public TransactionSellConfirmState(TrainCar selectedCar, string carID, float carSellPrice)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L("lo/radio/sselected/content", carID, carSellPrice.ToString()),
				actionText: LocalizationAPI.L("lo/radio/general/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;
			this.carID = carID;
			this.carSellPrice = carSellPrice;

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
				return new TransactionSellCancel(selectedCar, carID, carSellPrice);
			}

			TrainCar target = TrainCar.Resolve(hit.transform.root);

			if (target is null || target != selectedCar)
			{
				return new TransactionSellCancel(selectedCar, carID, carSellPrice);
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
