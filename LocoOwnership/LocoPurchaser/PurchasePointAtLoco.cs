using System;

using CommsRadioAPI;

namespace LocoOwnership.LocoPurchaser
{
	// This class inherits PointAtLocoState for the radio state
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

			// Add check to determine if player already has 10 of the same loco type, if yes
			// then pass to fail screen, implement later

			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new TransactionPurchaseConfirm(selectedCar, carID, carBuyPrice);
		}
	}
}
