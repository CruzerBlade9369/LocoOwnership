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

		private float carSellPrice;
		private bool highlighterState;
		private TrainCar selectedCar;

		public TransactionSellConfirm(TrainCar selectedCar, bool highlighterState)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L("lo/radio/sselected/content", selectedCar.ID, PricesCalc.CalculateSellPrice(selectedCar).ToString()),
				actionText: highlighterState
				? LocalizationAPI.L("comms/confirm")
				: LocalizationAPI.L("comms/cancel"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;
			this.highlighterState = highlighterState;

			if (this.selectedCar == null)
			{
				Main.DebugLog("selectedCar is null");
				throw new ArgumentNullException(nameof(selectedCar));
			}
		}

		private bool IsLocoDebtCleared()
		{
			TrainCar tender = CarGetters.GetTender(selectedCar);
			if (DebtHandling.CheckLocoDebtSell(selectedCar, tender))
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

			if (!OwnedLocosManager.HasLocoGUIDAsKey(selectedCar.CarGUID))
			{
				return new TransactionSellFail(1);
			}

			if(!IsLocoDebtCleared() && !Main.settings.advancedEco)
			{
				return new TransactionSellFail(0);
			}

			carSellPrice = PricesCalc.CalculateSellPrice(selectedCar);
			OwnedLocosManager.SellLoco(selectedCar);
			Inventory.Instance.AddMoney(carSellPrice);
			utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
			return new TransactionSellSuccess(selectedCar, carSellPrice);
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			RaycastHit hit;
			if (!Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out hit, SIGNAL_RANGE, CarHighlighter.trainCarMask))
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
			CarHighlighter.StartSelectorHighlighter(utility, selectedCar, highlighterState);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			CarHighlighter.StopSelectorHighlighter();
		}
	}
}
