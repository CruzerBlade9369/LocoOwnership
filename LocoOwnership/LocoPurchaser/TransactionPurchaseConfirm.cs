using System;

using DV.InventorySystem;

using CommsRadioAPI;
using LocoOwnership.Shared;

namespace LocoOwnership.LocoPurchaser
{
	// This class inherits PurchaseConfirmState for the radio state
	internal class TransactionPurchaseConfirm : TransactionPurchaseConfirmState
	{
		private float carBuyPrice;
		private double playerMoney;
		private string currentLicense;

		UnlockablesManager unlockManager;

		private OwnedLocos ownedLocosHandler;

		public TransactionPurchaseConfirm(TrainCar selectedCar, string carID, float carBuyPrice)
			: base(selectedCar, carID, carBuyPrice)
		{
			this.selectedCar = selectedCar;
			this.carBuyPrice = carBuyPrice;
			playerMoney = Inventory.Instance.PlayerMoney;

			unlockManager = new UnlockablesManager();
			ownedLocosHandler = new OwnedLocos();
			currentLicense = $"{selectedCar.carLivery.requiredLicense.v1}";
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			// Check if player does not have manual service
			if (!unlockManager.IsGeneralLicenseUnlocked("ManualService"))
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(2);
			}

			// Check if player does not have has license for loco
			if (!unlockManager.IsGeneralLicenseUnlocked(currentLicense) )
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(1);
			}

			// Check if player can afford
			if (playerMoney >= carBuyPrice)
			{
				bool purchaseSuccess = ownedLocosHandler.OnLocoBuy(selectedCar);
				if (!purchaseSuccess)
				{
					utility.PlaySound(VanillaSoundCommsRadio.Warning);
					return new TransactionPurchaseFail(3);
				}

				Inventory.Instance.RemoveMoney(carBuyPrice);
				utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
				return new TransactionPurchaseSuccess(selectedCar, carBuyPrice);
			}
			else
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionPurchaseFail(0);
			}
		}
	}
}
