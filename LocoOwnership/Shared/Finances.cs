using DV.ThingTypes;

namespace LocoOwnership.Shared
{
	internal class Finances
	{
		private float carBuyPrice;
		private float carSellPrice;

		public static Settings settings = new Settings();

		public float CalculateBuyPrice(TrainCar selectedCar)
		{
			if (settings.freeSandboxOwnership)
			{
				carBuyPrice = 0f;
			}
			else
			{
				if (selectedCar.carType == TrainCarType.LocoShunter)
				{
					carBuyPrice = 20000f;
				}
				else
				{
					carBuyPrice = selectedCar.carLivery.requiredLicense.price * 2f;
				}
			}
			return carBuyPrice;
		}

		public float CalculateSellPrice(TrainCar selectedCar)
		{
			if (settings.freeSandboxOwnership)
			{
				carSellPrice = 0f;
			}
			else
			{
				if (selectedCar.carType == TrainCarType.LocoShunter)
				{
					carSellPrice = 5000f;
				}
				else
				{
					carSellPrice = selectedCar.carLivery.requiredLicense.price / 2f;
				}
			}
			return carSellPrice;
		}
	}
}
