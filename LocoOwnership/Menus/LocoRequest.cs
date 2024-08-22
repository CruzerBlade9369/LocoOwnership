using System;

using UnityEngine;

using DV;
using DV.Localization;

using CommsRadioAPI;

using LocoOwnership.LocoRequester;

namespace LocoOwnership.Menus
{
	internal class LocoRequest : AStateBehaviour
	{
		public LocoRequest()
			: base(new CommsRadioState(
				titleText: /*LocalizationAPI.L("lo/radio/general/purchase")*/"request",
				contentText: /*LocalizationAPI.L("lo/radio/locopurchase/content")*/"request a locomotive?",
				actionText: LocalizationAPI.L("lo/radio/general/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			switch (action)
			{
				case InputAction.Activate:
					RequestLocoSelector.RefreshRequestableLocos();

					utility.PlaySound(VanillaSoundCommsRadio.ModeEnter);
					return new RequestLocoSelector(0);

				case InputAction.Up:
					return new LocoSell();

				case InputAction.Down:
					return new LocoPurchase();

				default:
					Debug.Log("Main menu error: why are you here?");
					throw new Exception($"Unexpected action: {action}");
			}
		}
	}
}
