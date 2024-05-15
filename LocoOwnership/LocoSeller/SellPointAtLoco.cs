using System;

using CommsRadioAPI;

namespace LocoOwnership.LocoSeller
{
	// This class inherits SellAtLocoState for the radio state
	internal class SellPointAtLoco : SellPointAtLocoState
	{

		private string carID;
		private float carSellPrice;

		public SellPointAtLoco(TrainCar selectedCar, string carID, float carSellPrice)
			: base(selectedCar, carID, carSellPrice)
		{
			this.carID = carID;
			this.carSellPrice = carSellPrice;
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new TransactionSellConfirm(selectedCar, carID, carSellPrice);
		}
	}
}
