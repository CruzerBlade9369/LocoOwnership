using DV.ThingTypes;
using DV.UserManagement;

namespace LocoOwnership.Shared
{
	public class Finances
	{
		public const float DE2_ARTIFICIAL_LICENSE_PRICE = 10000f;

		public float CalculateBuyPrice(TrainCar selectedCar)
		{
			float carBuyPrice;

			if (Main.settings.freeOwnership)
			{
				carBuyPrice = 0f;
				return carBuyPrice;
			}

			if (Main.settings.freeSandboxOwnership && UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
			{
				carBuyPrice = 0f;
			}
			else
			{
				if (selectedCar.carType == TrainCarType.LocoShunter)
				{
					carBuyPrice = DE2_ARTIFICIAL_LICENSE_PRICE * 2f;
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
			float carSellPrice;

			if (Main.settings.freeOwnership)
			{
				carSellPrice = 0f;
				return carSellPrice;
			}

			if (Main.settings.freeSandboxOwnership && UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
			{
				carSellPrice = 0f;
			}
			else
			{
				if (selectedCar.carType == TrainCarType.LocoShunter)
				{
					carSellPrice = DE2_ARTIFICIAL_LICENSE_PRICE / 2f;
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
