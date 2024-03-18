using System;
using CommsRadioAPI;
using DV;

namespace LocoOwnership.CommsRadioHandler.LocoPurchaser
{
	internal class PurchaseMenu : AStateBehaviour
	{
		public PurchaseMenu() : base(
			new CommsRadioState(
				titleText: "PURCHASE LOCO",
				contentText: "PURCHASE THIS LOCOMOTIVE?",
				buttonBehaviour: ButtonBehaviourType.Regular
			)
		)
		{ }

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			Main.DebugLog("Purchase menu OnEnter!");
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			utility.PlaySound(VanillaSoundCommsRadio.ModeEnter);
			Main.DebugLog("Purchase menu OnAction!");
			return action switch
			{
				_ => throw new ArgumentException(),
			};
		}
	}
}
