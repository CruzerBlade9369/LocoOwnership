using System;

using DV;
using DV.Localization;

using CommsRadioAPI;

using LocoOwnership.Menus;

namespace LocoOwnership.LocoPurchaser
{
	internal class TransactionPurchaseSuccess : AStateBehaviour
	{
		public TransactionPurchaseSuccess(TrainCar selectedCar, float buyPrice)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/purchase"),
				contentText: LocalizationAPI.L("lo/radio/psuccess/content", selectedCar.ID, buyPrice.ToString()),
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
			return new LocoPurchase();
		}
	}
}
