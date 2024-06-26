using System;

using UnityEngine;

using DV.InventorySystem;
using DV.ThingTypes;
using DV.ThingTypes.TransitionHelpers;

using CommsRadioAPI;
using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.LocoPurchaser
{
	// This class inherits TransactionPurchaseConfirmState for the radio state
	internal class TransactionPurchaseConfirm : TransactionPurchaseConfirmState
	{
		private float carBuyPrice;
		private double playerMoney;
		private GeneralLicenseType_v2 currentLicense;

		LicenseManager licenseManager;

		private OwnedLocos ownedLocosHandler;

		public TransactionPurchaseConfirm(TrainCar selectedCar, string carID, float carBuyPrice)
			: base(selectedCar, carID, carBuyPrice)
		{
			this.selectedCar = selectedCar;
			this.carBuyPrice = carBuyPrice;
			playerMoney = Inventory.Instance.PlayerMoney;

			licenseManager = LicenseManager.Instance;
			ownedLocosHandler = new OwnedLocos();
			currentLicense = selectedCar.carLivery.requiredLicense;
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
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
			if (!licenseManager.IsGeneralLicenseAcquired(currentLicense) )
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(1);
			}

			// Check if player can afford
			if (playerMoney >= carBuyPrice)
			{
				OwnedLocos.DebtHandlingResult purchaseSuccess = ownedLocosHandler.OnLocoBuy(selectedCar);
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
	}
}
