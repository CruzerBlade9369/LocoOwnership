using System;

using DV;
using DV.Localization;

using CommsRadioAPI;

using LocoOwnership.Menus;

namespace LocoOwnership.LocoRequester
{
	internal class RequestFail : AStateBehaviour
	{
		private static readonly string[] failReasons = [
			"lo/radio/pfail/content/0",
			"lo/radio/rfail/content/1",
			"lo/radio/rfail/content/2",
			"lo/radio/rfail/content/3",
		];

		public RequestFail(int failState)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/request"),
				contentText: LocalizationAPI.L(failReasons[failState]),
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				return this;
			}

			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new LocoRequest();
		}
	}
}
