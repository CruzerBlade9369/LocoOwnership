using System;

using DV;
using DV.Localization;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.LocoRequester;
using System.Linq;

namespace LocoOwnership.Menus
{
	internal class LocoRequest : AStateBehaviour
	{
		public LocoRequest()
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/request"),
				contentText: LocalizationAPI.L("lo/radio/locorequest/content"),
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			switch (action)
			{
				case InputAction.Activate:
					RequestLocoSelector.RefreshRequestableLocos();

					if (!RequestLocoSelector.requestableOwnedLocos.Any())
					{
						utility.PlaySound(VanillaSoundCommsRadio.Warning);
						return new RequestFail(1);
					}

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
