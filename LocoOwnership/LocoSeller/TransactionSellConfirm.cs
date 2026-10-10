using CommsRadioAPI;
using DV;
using DV.InventorySystem;
using DV.Localization;
using LocoOwnership.OwnershipHandler;
using LocoOwnership.Shared;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LocoOwnership.LocoSeller
{
	public class TransactionSellConfirm : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;

		private float carSellPrice;
		private bool aimingAtCar;
		private TrainCar selectedCar;

		public TransactionSellConfirm(TrainCar selectedCar, float carSellPrice, bool highlighterState)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L("lo/radio/sselected/content", selectedCar.ID, carSellPrice.ToString()),
				actionText: highlighterState
				? LocalizationAPI.L("comms/confirm")
				: LocalizationAPI.L("comms/cancel"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;
			this.aimingAtCar = highlighterState;
			this.carSellPrice = carSellPrice;

			if (this.selectedCar == null)
			{
				Main.DebugLog("selectedCar is null");
				throw new ArgumentNullException(nameof(selectedCar));
			}
		}

		private bool IsLocoDebtCleared()
		{
			List<TrainCar> trainSet = CarUtils.GetCCLTrainsetOrLocoAndTender(selectedCar);
			foreach (TrainCar car in trainSet) if (!DebtHandling.IsDebtClearForSell(car)) return false;
			return true;
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				return this;
			}

			if (!aimingAtCar)
			{
				utility.PlaySound(VanillaSoundCommsRadio.Cancel);
				return new SellPointAtNothing();
			}

			if (!CarUtils.IsLocoOrLocosetValid(selectedCar))
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionSellFail(1);
			}

			// Succeeded by the next block
			/*if (!OwnedLocosManager.Instance.IsLocoGuidAlreadyOwned(selectedCar.CarGUID))
			{
				return new TransactionSellFail(1);
			}*/

			if (!selectedCar.TryGetComponent<LocoOwnershipController>(out _))
			{
				return new TransactionSellFail(1);
			}

			if (!IsLocoDebtCleared() && !Main.Settings.advancedEco)
			{
				return new TransactionSellFail(0);
			}

			OwnedLocosManager.Instance.UnsetLocoFromOwned(selectedCar);
			selectedCar.GetComponent<LocoOwnershipController>();
			Inventory.Instance.AddMoney(carSellPrice);
			utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
			return new TransactionSellSuccess(selectedCar, carSellPrice);
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			RaycastHit hit;
			// If pointing away from selected loco, highlight red
			if (!Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out hit, SIGNAL_RANGE, CarHighlighter.trainCarMask))
			{
				if (aimingAtCar)
				{
					return new TransactionSellConfirm(selectedCar, carSellPrice, false);
				}

				return this;
			}

			TrainCar target = TrainCar.Resolve(hit.transform.root);
			if (target == null)
			{
				return this;
			}

			// If pointing at the selected loco, highlight blue
			if (target.CarGUID == selectedCar.CarGUID)
			{
				if (!aimingAtCar)
				{
					utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
					return new TransactionSellConfirm(selectedCar, carSellPrice, true);
				}
			}

			return this;
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			CarHighlighter.StartSelectorHighlighter(utility, selectedCar, aimingAtCar);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			CarHighlighter.StopSelectorHighlighter();
		}
	}
}
