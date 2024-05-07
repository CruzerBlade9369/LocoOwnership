using System;

using DV;

using UnityEngine;

using CommsRadioAPI;
using LocoOwnership.Menus;

namespace LocoOwnership.LocoPurchaser
{
	internal class TransactionPurchaseSuccess : AStateBehaviour
	{
		public TransactionPurchaseSuccess(TrainCar selectedCar, float buyPrice)
			: base(new CommsRadioState(
				titleText: "Purchase",
				contentText: $"Successfully purchased {selectedCar.ID} for ${buyPrice}.",
				actionText: "Confirm",
				buttonBehaviour: ButtonBehaviourType.Override))
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new LocoPurchase();
		}
	}
}
