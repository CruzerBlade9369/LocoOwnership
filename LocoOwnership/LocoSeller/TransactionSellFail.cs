using System;

using DV;

using CommsRadioAPI;
using LocoOwnership.Menus;

namespace LocoOwnership.LocoSeller
{
	internal class TransactionSellFail : AStateBehaviour
	{
		private static readonly string[] failReasons = {
			"fully service this locomotive before selling."
		};

		public TransactionSellFail(int failState)
			: base(new CommsRadioState(
				titleText: "Sell",
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
			return new LocoSell();
		}
	}
}
