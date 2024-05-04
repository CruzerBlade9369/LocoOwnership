using System;

using DV;

using CommsRadioAPI;
using LocoOwnership.LocoPurchaser;
using LocoOwnership.Menus;

namespace LocoOwnership.Menus
{
	internal class LocoPurchase : AStateBehaviour
	{
		public LocoPurchase()
			: base(new CommsRadioState(
				titleText: "Purchase",
				contentText: "Purchase a locomotive?",
				buttonBehaviour: ButtonBehaviourType.Override))
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			switch (action)
			{
				case InputAction.Activate:
					utility.PlaySound(VanillaSoundCommsRadio.Warning);
					return new PurchasePointAtNothing();

				case InputAction.Up:
					return new PlaySound();

				case InputAction.Down:
					return new LocoSell();

				default:
					Main.DebugLog("Main menu error: why are you here?");
					throw new Exception($"Unexpected action: {action}");
			}
		}
	}
}
