using System;

using DV.InventorySystem;

using CommsRadioAPI;

using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.LocoSeller
{
	// This class inherits TransactionSellConfirmState for the radio state
	internal class TransactionSellConfirm : TransactionSellConfirmState
	{
		private float carSellPrice;

		private OwnedLocos ownedLocosHandler;

		public TransactionSellConfirm(TrainCar selectedCar, string carID, float carSellPrice)
			: base(selectedCar, carID, carSellPrice)
		{
			this.selectedCar = selectedCar;
			this.carSellPrice = carSellPrice;

			ownedLocosHandler = new OwnedLocos();
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			OwnedLocos.DebtHandlingResult sellSuccess = ownedLocosHandler.OnLocoSell(selectedCar);
			if (sellSuccess.DebtNotZero)
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new TransactionSellFail(0);
			}
			else
			{
				Inventory.Instance.AddMoney(carSellPrice);
				utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
				return new TransactionSellSuccess(selectedCar, carSellPrice);
			}
		}
	}
}
