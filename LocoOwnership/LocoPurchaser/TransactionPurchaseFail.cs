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
			"you can't buy any more locomotives of this type!"
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
			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new LocoPurchase();
		}
	}
}
