using System;

using CommsRadioAPI;

namespace LocoOwnership.LocoSeller
{
	// This class inherits PurchaseCancelState for the radio state
	internal class TransactionSellCancel : TransactionSellCancelState
	{
		public TransactionSellCancel(TrainCar selectedCar, string carID, float carSellPrice)
			: base(selectedCar, carID, carSellPrice)
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			utility.PlaySound(VanillaSoundCommsRadio.Cancel);
			return new SellPointAtNothing();
		}
	}
}
