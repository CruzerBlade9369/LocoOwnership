using System;

using DV;
using DV.Localization;
using DV.InventorySystem;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Shared;
using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.LocoSeller
{
	public class TransactionSellConfirm : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;

		private int trainCarMask;
		private float carSellPrice;
		private bool highlighterState;
		private TrainCar selectedCar;

		private Transform signalOrigin;
		private CommsRadioCarDeleter carDeleter;
		private CarHighlighter highlighter;

		public TransactionSellConfirm(TrainCar selectedCar, bool highlighterState)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L("lo/radio/sselected/content", selectedCar.ID, Finances.CalculateSellPrice(selectedCar).ToString()),
				actionText: highlighterState
				? LocalizationAPI.L("comms/confirm")
				: LocalizationAPI.L("comms/cancel"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;
			this.highlighterState = highlighterState;

			if (highlighter == null)
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

		private bool IsLocoDebtCleared()
		{
			TrainCar tender = CarGetters.GetTender(selectedCar);
			if (DebtHandling.RemoveOwnedVehicle(selectedCar, tender))
			{
				return true;
			}

			return false;
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				return this;
			}

			if (!highlighterState)
			{
				utility.PlaySound(VanillaSoundCommsRadio.Cancel);
				return new SellPointAtNothing();
			}

			if (!OwnedLocos.HasLocoGUIDAsKey(selectedCar.CarGUID))
			{
				return new TransactionSellFail(1);
			}

			if(!IsLocoDebtCleared())
			{
				return new TransactionSellFail(0);
			}

			carSellPrice = Finances.CalculateSellPrice(selectedCar);
			OwnedLocos.SellLoco(selectedCar);
			Inventory.Instance.AddMoney(carSellPrice);
			utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
			return new TransactionSellSuccess(selectedCar, carSellPrice);
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			while (signalOrigin == null)
			{
				Main.DebugLog("signalOrigin is null for some reason");
				RefreshRadioComponent();
			}

			RaycastHit hit;
			if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out hit, SIGNAL_RANGE, trainCarMask))
			{
				if (highlighterState)
				{
					return new TransactionSellConfirm(selectedCar, false);
				}

				return this;
			}

			TrainCar target = TrainCar.Resolve(hit.transform.root);
			if (target == null)
			{
				return this;
			}

			// if pointing at the selected locomotive
			if (target.CarGUID == selectedCar.CarGUID && selectedCar.carLivery.requiredLicense != null)
			{
				if (!highlighterState)
				{
					utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
					return new TransactionSellConfirm(selectedCar, true);
				}
			}

			return this;
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			highlighter.InitHighlighter(selectedCar, carDeleter);
			highlighter.StartHighlighter(utility, highlighterState);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			highlighter.StopHighlighter();
		}
	}
}
