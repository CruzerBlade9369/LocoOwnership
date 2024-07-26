using System;

using DV;
using DV.Localization;

using CommsRadioAPI;
using LocoOwnership.Menus;

namespace LocoOwnership.LocoSeller
{
	internal class TransactionSellFail : AStateBehaviour
	{
		private static readonly string[] failReasons = {
			"lo/radio/sfail/content/0"
		};

		public TransactionSellFail(int failState)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L(failReasons[failState]),
				actionText: LocalizationAPI.L("lo/radio/general/confirm"),
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
			return new LocoSell();
		}
	}
}
