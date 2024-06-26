using System;

using DV;

using CommsRadioAPI;
using LocoOwnership.Menus;

namespace LocoOwnership.LocoPurchaser
{
	internal class TransactionPurchaseFail : AStateBehaviour
	{
		private static readonly string[] failReasons = {
			"insufficient funds.",
			"you do not have the required license for this locomotive.",
			"manual service license is required to own locomotives.",
			"you have reached the maximum number of owned locomotives.",
			"pay the fees of this locomotive before purchasing.",
			"cannot purchase player spawned locomotives."
		};

		public TransactionPurchaseFail(int failState)
			: base(new CommsRadioState(
				titleText: "Purchase",
				contentText: $"{failReasons[failState]}",
				actionText: "Confirm",
				buttonBehaviour: ButtonBehaviourType.Override))
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new LocoPurchase();
		}
	}
}
