using System;

using DV;
using DV.Localization;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Shared;

namespace LocoOwnership.LocoSeller
{
	internal abstract class TransactionSellCancelState : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;

		internal TrainCar selectedCar;
		private Transform signalOrigin;
		private CommsRadioCarDeleter carDeleter;
		private int trainCarMask;

		private string carID;
		private float carSellPrice;

		private CarHighlighter highlighter;

		public TransactionSellCancelState(TrainCar selectedCar, string carID, float carSellPrice)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L("lo/radio/sselected/content", carID, carSellPrice.ToString()),
				actionText: LocalizationAPI.L("lo/radio/general/cancel"),
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

		private void refreshSignalOriginAndTrainCarMask()
		{
			trainCarMask = highlighter.RefreshTrainCarMask();
			signalOrigin = highlighter.RefreshSignalOrigin();
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

			// Try to get the train car we're pointing at
			TrainCar nextCar = TrainCar.Resolve(hit.transform.root);

			// If we aren't pointing at a car
			if (selectedCar is null)
			{
				return this;
			}

			// If we're pointing at the previous locomotive
			if (nextCar.ID == selectedCar.ID)
			{
				if (selectedCar.carLivery.requiredLicense is not null)
				{
					utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
					return new TransactionSellConfirm(selectedCar, carID, carSellPrice);
				}
			}
			else
			{
				return this;
			}

			return this;
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			trainCarMask = highlighter.RefreshTrainCarMask();
			highlighter.StartHighlighter(utility, previous, false);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			highlighter.StopHighlighter(utility, next);
		}
	}
}
