using System;

using CommsRadioAPI;

namespace LocoOwnership.LocoPurchaser
{
	// This class inherits PurchasePointAtLocoState for the radio state
	internal class PurchasePointAtLoco : PurchasePointAtLocoState
	{
		private string carID;
		private float carBuyPrice;

		public PurchasePointAtLoco(TrainCar selectedCar, string carID, float carBuyPrice)
			: base(selectedCar, carID, carBuyPrice)
		{
			this.carID = carID;
			this.carBuyPrice = carBuyPrice;
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new TransactionPurchaseConfirm(selectedCar, carID, carBuyPrice);
		}
	}
}
