using System;

using DV;
using DV.Localization;
using DV.InventorySystem;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Shared;
using LocoOwnership.OwnershipHandler;
using LocoOwnership.Menus;

namespace LocoOwnership.LocoSeller
{
	internal class TransactionSellConfirm : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;

		private int trainCarMask;
		private float carSellPrice;

		private bool highlighterState;

		internal TrainCar selectedCar;
		private Transform signalOrigin;
		private CommsRadioCarDeleter carDeleter;
		private CarHighlighter highlighter;

		public TransactionSellConfirm(
			TrainCar selectedCar,
			float carSellPrice,
			CommsRadioCarDeleter carDeleter,
			CarHighlighter highlighter,
			bool highlighterState
			)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L("lo/radio/sselected/content", selectedCar.ID, carSellPrice.ToString()),
				actionText: highlighterState
				? LocalizationAPI.L("lo/radio/general/confirm")
				: LocalizationAPI.L("lo/radio/general/cancel"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;
			this.carSellPrice = carSellPrice;
			this.carDeleter = carDeleter;
			this.highlighter = highlighter;
			this.highlighterState = highlighterState;

			signalOrigin = carDeleter.signalOrigin;

			if (this.selectedCar is null)
			{
				Main.DebugLog("selectedCar is null");
				throw new ArgumentNullException(nameof(selectedCar));
			}

			highlighter.InitHighlighter(selectedCar, carDeleter);
			RefreshRadioComponent();
		}

		private void RefreshRadioComponent()
		{
			trainCarMask = highlighter.RefreshTrainCarMask();
			carDeleter = highlighter.RefreshCarDeleterComponent();
			signalOrigin = carDeleter.signalOrigin;
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			if (!highlighterState)
			{
				return new SellPointAtNothing();
			}

			OwnedLocos.DebtHandlingResult sellSuccess = Main.ownershipHandler.OnLocoSell(selectedCar);
			if (sellSuccess.DebtNotZero)
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionSellFail(0);
			}
			else
			{
				Inventory.Instance.AddMoney(carSellPrice);
				utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
				return new TransactionSellSuccess(selectedCar, carSellPrice);
			}
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			while (signalOrigin is null)
			{
				Main.DebugLog("signalOrigin is null for some reason");
				RefreshRadioComponent();
			}

			RaycastHit hit;

			// If we're not pointing at anything
			if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out hit, SIGNAL_RANGE, trainCarMask))
			{
				if (highlighterState)
				{
					return new TransactionSellConfirm(selectedCar, carSellPrice, carDeleter, highlighter, false);
				}

				return this;
			}

			// Try to get the train car we're pointing at
			TrainCar target = TrainCar.Resolve(hit.transform.root);

			if (selectedCar is null)
			{
				return this;
			}

			// If we're pointing at the same locomotive
			if (target.ID == selectedCar.ID && selectedCar.carLivery.requiredLicense is not null)
			{
				if (!highlighterState)
				{
					utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
					return new TransactionSellConfirm(selectedCar, carSellPrice, carDeleter, highlighter, true);
				}
			}

			return this;
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			trainCarMask = highlighter.RefreshTrainCarMask();
			highlighter.StartHighlighter(utility, highlighterState);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			highlighter.StopHighlighter();
		}
	}
}
