using DV;
using DV.Localization;

using CommsRadioAPI;

using LocoOwnership.Menus;

namespace LocoOwnership.LocoPurchaser
{
	public class TransactionPurchaseFail : AStateBehaviour
	{
		private static readonly string[] failReasons = [
			"lo/radio/pfail/content/0",
			"lo/radio/pfail/content/1",
			"lo/radio/pfail/content/2",
			"lo/radio/pfail/content/3",
			"lo/radio/pfail/content/4",
			"lo/radio/pfail/content/5",
			"lo/radio/pfail/content/6",
			"lo/radio/pfail/content/7",
		];

		public TransactionPurchaseFail(int failState)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/purchase"),
				contentText: LocalizationAPI.L(failReasons[failState]),
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				return this;
			}

			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new LocoPurchase();
		}
	}
}
