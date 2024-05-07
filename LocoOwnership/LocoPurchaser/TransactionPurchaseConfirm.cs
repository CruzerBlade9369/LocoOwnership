using System;

using DV.InventorySystem;

using CommsRadioAPI;

using LocoOwnership.Shared;

namespace LocoOwnership.LocoPurchaser
{
	// This class inherits PurchaseConfirmState for the radio state
	internal class TransactionPurchaseConfirm : TransactionPurchaseConfirmState
	{
		private float getBuyPrice;
		private double playerMoney;

		public TransactionPurchaseConfirm(TrainCar selectedCar, string carID, float carBuyPrice)
			: base(selectedCar, carID, carBuyPrice)
		{
			getBuyPrice = carBuyPrice;
			playerMoney = Inventory.Instance.PlayerMoney;
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			
			if (playerMoney >= getBuyPrice)
			{
				// Player can afford
				utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
				return new PurchasePointAtNothing();
				// implement continuation of can afford
			}
			else
			{
				// Player cannot afford
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new PurchasePointAtNothing();
				// implement continuation of cannot afford
			}

			
			
		}
	}
}
