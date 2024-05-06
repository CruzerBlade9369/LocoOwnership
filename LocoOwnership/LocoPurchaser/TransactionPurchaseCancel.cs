using System;

using CommsRadioAPI;

namespace LocoOwnership.LocoPurchaser
{
	internal class TransactionPurchaseCancel : TransactionPurchaseCancelState
	{
		public TransactionPurchaseCancel(TrainCar selectedCar) : base(selectedCar)
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
