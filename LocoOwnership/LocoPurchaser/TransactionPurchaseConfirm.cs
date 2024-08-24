using System;

using DV;
using DV.Localization;
using DV.ThingTypes;
using DV.InventorySystem;
using DV.ThingTypes.TransitionHelpers;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Shared;
using LocoOwnership.OwnershipHandler;
using LocoOwnership.Menus;

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
				? LocalizationAPI.L("lo/radio/general/confirm")
				: LocalizationAPI.L("lo/radio/general/cancel"),
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

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			// Cancel when not looking at loco
			if (!highlighterState)
			{
				return new PurchasePointAtNothing();
			}

			// Check if loco is player spawned
			if (selectedCar.playerSpawnedCar)
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(5);
			}

			// Check if player does not have manual service
			if (!licenseManager.IsGeneralLicenseAcquired(GeneralLicenseType.ManualService.ToV2()))
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(2);
			}

			// Check if player does not have has license for loco
			if (!licenseManager.IsGeneralLicenseAcquired(currentLicense))
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(1);
			}

			// Check if player can afford
			if (playerMoney >= carBuyPrice)
			{
				OwnedLocos.DebtHandlingResult purchaseSuccess = Main.ownershipHandler.OnLocoBuy(selectedCar);
				if (purchaseSuccess.MaxOwnedLoc)
				{
					utility.PlaySound(VanillaSoundCommsRadio.Warning);
					return new TransactionPurchaseFail(3);
				}
				else if (purchaseSuccess.DebtNotZero)
				{
					utility.PlaySound(VanillaSoundCommsRadio.Warning);
					return new TransactionPurchaseFail(4);
				}

				// Success
				Inventory.Instance.RemoveMoney(carBuyPrice);
				utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
				return new TransactionPurchaseSuccess(selectedCar, carBuyPrice);
			}
			else
			{
				// Broke ahh
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(0);
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
					return new TransactionPurchaseConfirm(selectedCar, carBuyPrice, carDeleter, highlighter, false);
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
