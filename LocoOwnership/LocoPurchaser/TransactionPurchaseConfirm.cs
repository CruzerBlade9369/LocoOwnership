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
		private double playerMoney;
		private bool highlighterState;
		private TrainCar selectedCar;

		private int trainCarMask;
		private Transform signalOrigin;
		private CommsRadioCarDeleter carDeleter;
		private CarHighlighter highlighter;

		private GeneralLicenseType_v2 currentLicense;
		LicenseManager licenseManager;

		public TransactionPurchaseConfirm(TrainCar selectedCar, bool highlighterState)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/purchase"),
				contentText: LocalizationAPI.L("lo/radio/pselected/content", selectedCar.ID, Finances.CalculateBuyPrice(selectedCar).ToString()),
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

			carBuyPrice = Finances.CalculateBuyPrice(selectedCar);
			playerMoney = Inventory.Instance.PlayerMoney;
			licenseManager = LicenseManager.Instance;
			currentLicense = selectedCar.carLivery.requiredLicense;

			RefreshRadioComponent();
		}

		private void RefreshRadioComponent()
		{
			trainCarMask = CarHighlighter.RefreshTrainCarMask();
			carDeleter = CarHighlighter.RefreshCarDeleterComponent();
			signalOrigin = carDeleter.signalOrigin;
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
			int maxOwnedLocos = Main.settings.maxLocosLimit;
			if (OwnedLocos.CountLocosOnly() >= maxOwnedLocos)
			{
				return true;
			}

			return false;
		}

		private bool IsLocoDebtCleared()
		{
			TrainCar tender = CarGetters.GetTender(selectedCar);
			if (DebtHandling.SetVehicleToOwned(selectedCar, tender))
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

			if (!licenseManager.IsGeneralLicenseAcquired(GeneralLicenseType.ManualService.ToV2()))
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(2);
			}

			if (!licenseManager.IsGeneralLicenseAcquired(currentLicense))
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(1);
			}

			if (!HasDemonstrator())
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(6);
			}

			if (OwnedLocos.HasLocoGUIDAsKey(selectedCar.CarGUID))
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(7);
			}

			if (playerMoney < carBuyPrice)
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

			OwnedLocos.BuyLoco(selectedCar);
			Inventory.Instance.RemoveMoney(carBuyPrice);
			utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
			return new TransactionPurchaseSuccess(selectedCar, carBuyPrice);
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
