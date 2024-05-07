using System;

using CommsRadioAPI;

namespace LocoOwnership.LocoPurchaser
{
	// This class inherits PurchaseConfirmState for the radio state
	internal class TransactionPurchaseConfirm : TransactionPurchaseConfirmState
	{
		public TransactionPurchaseConfirm(TrainCar selectedCar, string carID, float carBuyPrice)
			: base(selectedCar, carID, carBuyPrice)
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
			return new PurchasePointAtNothing();
		}
	}
}
