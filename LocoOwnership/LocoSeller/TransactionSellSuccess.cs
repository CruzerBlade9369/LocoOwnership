using DV;
using DV.Localization;

using CommsRadioAPI;

using LocoOwnership.Menus;

namespace LocoOwnership.LocoSeller
{
	public class TransactionSellSuccess : AStateBehaviour
	{
		public TransactionSellSuccess(TrainCar selectedCar, float sellPrice)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/sell"),
				contentText: LocalizationAPI.L("lo/radio/ssuccess/content", selectedCar.ID, sellPrice.ToString()),
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
			return new OwnershipMenus(1);
		}
	}
}
