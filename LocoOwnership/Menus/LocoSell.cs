using System;

using DV;

using CommsRadioAPI;
using LocoOwnership.LocoSeller;
using LocoOwnership.Menus;

namespace LocoOwnership.Menus
{
	internal class LocoSell : AStateBehaviour
	{
		public LocoSell()
			: base(new CommsRadioState(
				titleText: "Sell",
				contentText: "Sell one of your locomotives?",
				buttonBehaviour: ButtonBehaviourType.Override))
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			switch (action)
			{
				case InputAction.Activate:
					utility.PlaySound(VanillaSoundCommsRadio.Warning);
					return new SellPointAtNothing();

				case InputAction.Up:
					return new LocoPurchase();

				case InputAction.Down:
					return new PlaySound();

				default:
					Main.DebugLog("Main menu error: why are you here?");
					throw new Exception($"Unexpected action: {action}");
			}
		}
	}
}
