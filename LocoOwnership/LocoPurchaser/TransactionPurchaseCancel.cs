using System;

using CommsRadioAPI;

namespace LocoOwnership.LocoPurchaser
{
	// This class inherits PurchaseCancelState for the radio state
	internal class TransactionPurchaseCancel : TransactionPurchaseCancelState
	{
		public TransactionPurchaseCancel(TrainCar selectedCar, string carID, float carBuyPrice)
			: base(selectedCar, carID, carBuyPrice)
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			utility.PlaySound(VanillaSoundCommsRadio.Cancel);
			return new PurchasePointAtNothing();
		}
	}
}
