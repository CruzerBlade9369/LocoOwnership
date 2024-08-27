using System;

using DV;
using DV.Localization;

using CommsRadioAPI;

using LocoOwnership.Menus;

namespace LocoOwnership.LocoRequester
{
	internal class RequestFail : AStateBehaviour
	{
		private static readonly string[] failReasons = {
			"insufficient funds.",
			"you do not own any locomotives yet.",
			"unable to deliver the requested locomotive.",
			"the requested locomotive needs to be on the rails for delivery.",
		};

		public RequestFail(int failState)
			: base(new CommsRadioState(
				titleText: "request",
				contentText: failReasons[failState],
				actionText: LocalizationAPI.L("lo/radio/general/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new LocoRequest();
		}
	}
}
