using DV.InventorySystem;

namespace LocoOwnership.Shared
{
	internal class Finances
	{
		private float carBuyPrice;
		private float carSellPrice;

		public static Settings settings = new Settings();

		public float CalculateBuyPrice(TrainCar selectedCar)
		{
			if (settings.freeSandboxOwnership is true)
			{
				carBuyPrice = 0f;
			}
			else
			{
				carBuyPrice = selectedCar.carLivery.requiredLicense.price * 2f;
			}
			return carBuyPrice;
		}

		public float CalculateSellPrice(TrainCar selectedCar)
		{
			if (settings.freeSandboxOwnership is true)
			{
				carSellPrice = 0f;
			}
			else
			{
				carSellPrice = selectedCar.carLivery.requiredLicense.price / 2f;
			}
			return carSellPrice;
		}
	}
}
