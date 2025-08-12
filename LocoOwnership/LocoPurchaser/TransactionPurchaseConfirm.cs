using System;

using DV;
using DV.Localization;
using DV.ThingTypes;
using DV.InventorySystem;
using DV.ThingTypes.TransitionHelpers;
using DV.UserManagement;
using static DV.LocoRestoration.LocoRestorationController;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Shared;
using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.LocoPurchaser
{
	public class TransactionPurchaseConfirm : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;

		private float carBuyPrice;
		private bool highlighterState;
		private TrainCar selectedCar;

		public TransactionPurchaseConfirm(TrainCar selectedCar, bool highlighterState = true)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/purchase"),
				contentText: LocalizationAPI.L("lo/radio/pselected/content", selectedCar.ID, PricesCalc.CalculateBuyPrice(selectedCar).ToString()),
				actionText: highlighterState
				? LocalizationAPI.L("comms/confirm")
				: LocalizationAPI.L("comms/cancel"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;
			this.highlighterState = highlighterState;

			if (this.selectedCar == null)
			{
				throw new ArgumentNullException(nameof(selectedCar));
			}

			carBuyPrice = PricesCalc.CalculateBuyPrice(selectedCar);
		}

		private bool HasDemonstrator()
		{
			if (Main.settings.skipDemonstrator)
			{
				return true;
			}

			if (UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
			{
				return true;
			}

			var controller = allLocoRestorationControllers.Find(x => x.locoLivery == selectedCar.carLivery);
			if (controller != null && controller.State >= RestorationState.S9_LocoServiced)
			{
				return true;
			}

			return false;
		}

		private bool HasEnoughLocos()
		{
			if (OwnedLocosManager.CountLocosOnly() >= Main.settings.maxLocosLimit)
			{
				return true;
			}

			return false;
		}

		private bool IsLocoDebtCleared()
		{
			TrainCar tender = CarGetters.GetTender(selectedCar);
			if (DebtHandling.CheckLocoDebtBuy(selectedCar, tender))
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
				return new PurchasePointAtNothing();
			}

			if (selectedCar.playerSpawnedCar)
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(5);
			}

			if (!LicenseManager.Instance.IsGeneralLicenseAcquired(GeneralLicenseType.ManualService.ToV2()))
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(2);
			}

			if (!LicenseManager.Instance.IsGeneralLicenseAcquired(selectedCar.carLivery.requiredLicense))
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(1);
			}

			if (!HasDemonstrator())
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(6);
			}

			if (OwnedLocosManager.HasLocoGUIDAsKey(selectedCar.CarGUID))
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(7);
			}

			if (Inventory.Instance.PlayerMoney < carBuyPrice)
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(0);
			}

			if (HasEnoughLocos())
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(3);
			}

			if (!IsLocoDebtCleared())
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(4);
			}

			OwnedLocosManager.BuyLoco(selectedCar);
			Inventory.Instance.RemoveMoney(carBuyPrice);
			utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
			return new TransactionPurchaseSuccess(selectedCar, carBuyPrice);
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			RaycastHit hit;
			if (!Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out hit, SIGNAL_RANGE, CarHighlighter.trainCarMask))
			{
				// if no longer looking at the locomotive
				if (highlighterState)
				{
					return new TransactionPurchaseConfirm(selectedCar, false);
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
					return new TransactionPurchaseConfirm(selectedCar, true);
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
