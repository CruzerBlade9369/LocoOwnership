using System;

using DV;
using DV.Localization;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.LocoSeller;

namespace LocoOwnership.Menus
{
	internal class LocoSell : AStateBehaviour
	{
		public LocoSell()
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L("lo/radio/locosell/content"),
				actionText: LocalizationAPI.L("lo/radio/general/confirm"),
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
					return new LocoRequest();

				default:
					Debug.Log("Main menu error: why are you here?");
					throw new Exception($"Unexpected action: {action}");
			}
		}
	}
}
