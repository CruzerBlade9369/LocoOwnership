using DV;

using CommsRadioAPI;
using LocoOwnership.Menus;

namespace LocoOwnership.LocoSeller
{
	internal class TransactionSellSuccess : AStateBehaviour
	{
		public TransactionSellSuccess(TrainCar selectedCar, float sellPrice)
			: base(new CommsRadioState(
				titleText: "Sell",
				contentText: $"Successfully sold {selectedCar.ID} for ${sellPrice}.",
				actionText: "Confirm",
				buttonBehaviour: ButtonBehaviourType.Override))
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new LocoSell();
		}
	}
}
