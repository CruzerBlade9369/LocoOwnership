using System;
using System.Linq;

using DV;
using DV.Localization;
using DV.ThingTypes;
using DV.InventorySystem;
using DV.ThingTypes.TransitionHelpers;
using static DV.LocoRestoration.LocoRestorationController;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Shared;
using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.LocoPurchaser
{
	internal class TransactionPurchaseConfirm : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;

		private int trainCarMask;
		private float carBuyPrice;
		private double playerMoney;

		private bool highlighterState;

		internal TrainCar selectedCar;
		private Transform signalOrigin;
		private CommsRadioCarDeleter carDeleter;
		private CarHighlighter highlighter;

		private GeneralLicenseType_v2 currentLicense;
		LicenseManager licenseManager;

		public TransactionPurchaseConfirm(
			TrainCar selectedCar,
			float carBuyPrice,
			CommsRadioCarDeleter carDeleter,
			CarHighlighter highlighter,
			bool highlighterState
			)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/purchase"),
				contentText: LocalizationAPI.L("lo/radio/pselected/content", selectedCar.ID, carBuyPrice.ToString()),
				actionText: highlighterState
				? LocalizationAPI.L("comms/confirm")
				: LocalizationAPI.L("comms/cancel"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;
			this.carBuyPrice = carBuyPrice;
			this.carDeleter = carDeleter;
			this.highlighter = highlighter;
			this.highlighterState = highlighterState;

			signalOrigin = carDeleter.signalOrigin;
			playerMoney = Inventory.Instance.PlayerMoney;
			licenseManager = LicenseManager.Instance;
			currentLicense = selectedCar.carLivery.requiredLicense;

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

		private bool HasDemonstrator()
		{
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
			if (OwnedLocos.ownedLocos.Values.Count(v => v.StartsWith("L-")) > maxOwnedLocos)
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

			if (OwnedLocos.ownedLocos.ContainsKey(selectedCar.CarGUID))
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

			// Success
			OwnedLocos.BuyLoco(selectedCar);
			Inventory.Instance.RemoveMoney(carBuyPrice);
			utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
			return new TransactionPurchaseSuccess(selectedCar, carBuyPrice);
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
					return new TransactionPurchaseConfirm(selectedCar, carBuyPrice, carDeleter, highlighter, false);
				}

				return this;
			}

			// Try to get the train car we're pointing at
			TrainCar target = TrainCar.Resolve(hit.transform.root);

			if (target is null)
			{
				return this;
			}

			// If we're pointing at the same locomotive
			if (target.ID == selectedCar.ID && selectedCar.carLivery.requiredLicense is not null)
			{
				if (!highlighterState)
				{
					utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
					return new TransactionPurchaseConfirm(selectedCar, carBuyPrice, carDeleter, highlighter, true);
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
