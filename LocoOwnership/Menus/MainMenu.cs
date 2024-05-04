using System;

using DV;

using CommsRadioAPI;
namespace LocoOwnership.Menus
{
	internal class MainMenu : AStateBehaviour
	{
		public MainMenu()
			: base(new CommsRadioState(
				titleText: "Ownership",
				contentText: "Manage owned locomotives.",
				buttonBehaviour: ButtonBehaviourType.Regular))
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
