using System;

using CommsRadioAPI;

namespace LocoOwnership.LocoPurchaser
{
	internal class TransactionPurchaseConfirm : TransactionPurchaseConfirmState
	{
		private string carID;
		public TransactionPurchaseConfirm(TrainCar selectedCar) : base(selectedCar)
		{
			carID = selectedCar.ID;
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
			Main.DebugLog($"Purchased L-{carID} for $###");
			return new PurchasePointAtNothing();
		}
	}
}
