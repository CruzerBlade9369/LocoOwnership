using DV.ThingTypes;
using DV.UserManagement;

namespace LocoOwnership.Shared
{
	internal class Finances
	{
		private const float DE2_ARTIFICIAL_LICENSE_PRICE = 10000f;
		private float carBuyPrice;
		private float carSellPrice;
		private float carLicensePrice;

		public static Settings settings = new Settings();

		public float CalculateBuyPrice(TrainCar selectedCar)
		{
			if (settings.freeOwnership)
			{
				carBuyPrice = 0f;
				return carBuyPrice;
			}

			if (settings.freeSandboxOwnership && UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
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
			if (settings.freeOwnership)
			{
				carSellPrice = 0f;
				return carSellPrice;
			}

			if (settings.freeSandboxOwnership && UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
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

		public float MissingCarsRefund(string locoType)
		{
			if (settings.freeOwnership)
			{
				carLicensePrice = 0f;
				return carLicensePrice;
			}

			if (settings.freeSandboxOwnership && UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
			{
				carLicensePrice = 0f;
			}
			else
			{
				if (locoType == "LocoShunter")
				{
					carLicensePrice = DE2_ARTIFICIAL_LICENSE_PRICE;
				}
				else
				{
					
				}
			}
			return carLicensePrice;
		}
	}
}
