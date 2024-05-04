using System;

using DV;

using CommsRadioAPI;
using LocoOwnership.PlaySound;
namespace LocoOwnership.Menus
{
	internal class PlaySound : AStateBehaviour
	{
		public PlaySound()
			: base(new CommsRadioState(
				titleText: "Playsound",
				contentText: "Test comms radio sounds.",
				buttonBehaviour: ButtonBehaviourType.Override))
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			switch (action)
			{
				case InputAction.Activate:
					utility.PlaySound(VanillaSoundCommsRadio.Warning);
					return new SoundPlayer(0);

				case InputAction.Up:
					return new LocoSell();

				case InputAction.Down:
					return new LocoPurchase();

				default:
					Main.DebugLog("Main menu error: why are you here?");
					throw new Exception($"Unexpected action: {action}");
			}
		}
	}
}
