using System;

using DV;
using DV.Localization;

using CommsRadioAPI;

namespace LocoOwnership.Menus
{
	internal class MainMenu : AStateBehaviour
	{
		public MainMenu()
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/mainmenu/title"),
				contentText: LocalizationAPI.L("lo/radio/mainmenu/content"),
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
