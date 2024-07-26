using System;

using DV;
using DV.Localization;

using CommsRadioAPI;
using LocoOwnership.LocoPurchaser;

namespace LocoOwnership.Menus
{
	internal class LocoPurchase : AStateBehaviour
	{
		public LocoPurchase()
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/purchase"),
				contentText: LocalizationAPI.L("lo/radio/locopurchase/content"),
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
					return new PurchasePointAtNothing();

				case InputAction.Up:
					return new LocoSell();

				case InputAction.Down:
					return new LocoSell();

				default:
					Main.DebugLog("Main menu error: why are you here?");
					throw new Exception($"Unexpected action: {action}");
			}
		}
	}
}
