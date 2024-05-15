using System;

using DV.InventorySystem;

using CommsRadioAPI;
using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.LocoSeller
{
	// This class inherits PurchaseConfirmState for the radio state
	internal class TransactionSellConfirm : TransactionSellConfirmState
	{
		private float carSellPrice;
		private double playerMoney;
		private string currentLicense;

		UnlockablesManager unlockManager;

		private OwnedLocos ownedLocosHandler;

		public TransactionSellConfirm(TrainCar selectedCar, string carID, float carSellPrice)
			: base(selectedCar, carID, carSellPrice)
		{
			this.selectedCar = selectedCar;
			this.carSellPrice = carSellPrice;
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

			bool sellSuccess = ownedLocosHandler.OnLocoSell(selectedCar);
			if (sellSuccess)
			{
				utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
				return new TransactionSellSuccess(selectedCar, carSellPrice);
			}
		}
	}
}
