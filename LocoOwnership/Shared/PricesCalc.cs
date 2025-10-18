using DV.Damage;
using DV.PointSet;
using DV.ThingTypes;
using DV.UserManagement;
using LocoOwnership.OwnershipHandler;
using System.Collections.Generic;
using UnityEngine;
using static DV.CommsRadioCrewVehicle;

namespace LocoOwnership.Shared
{
	public class PricesCalc
	{
		private const float DE2_ARTIFICIAL_LICENSE_PRICE = 10000f;
		private const float MIN_TELE_PRICE = 5000f;

		public static float CalculateBuyPrice(TrainCar selectedCar, bool getTotalTrainsetPrice = false)
		{

			if (Main.Settings.freeOwnership)
			{
				return 0f;
			}

			if (Main.Settings.freeSandboxOwnership && UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
			{
				return 0f;
			}

			// when using catalog price
			if (Main.Settings.locoCatPrices)
			{
				return GetPriceWhenBuyingWithCatalog(selectedCar, getTotalTrainsetPrice);
			}

			// when using license price, split the price and distribute between all units
			return GetPriceWhenBuyingWithLicense(selectedCar, getTotalTrainsetPrice);
		}

		public static float CalculateSellPrice(TrainCar selectedCar)
		{
			if (Main.Settings.freeOwnership)
			{
				return 0f;
			}

			if (Main.Settings.freeSandboxOwnership && UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
			{
				return 0f;
			}

			// legacy system revised, now new locos purchased with license price gets the price split up between all units

			if (Main.Settings.advancedEco)
			{
				return GetTotalTrainsetSellPrice(selectedCar, considerWear: true);
			}

			return GetTotalTrainsetSellPrice(selectedCar, considerWear: false) / Main.Settings.priceMultiplier;
		}

		public static float CalculateCarTeleportPrice(TrainCar selectedCar, EquiPointSet.Point? selectedPoint)
		{
			if (Main.Settings.freeCarTeleport)
			{
				return 0f;
			}

			Vector3 spawnPos = (Vector3)selectedPoint.Value.position + WorldMover.currentMove;
			float teleDistanceKm = Vector3.Distance(selectedCar.transform.position, spawnPos) * 0.001f;

			if (teleDistanceKm < 2f )
			{
				return MIN_TELE_PRICE;
			}

			return ((teleDistanceKm - 2f) * Main.Settings.requestPriceRate) + MIN_TELE_PRICE;
		}

		private static float GetPriceWhenBuyingWithCatalog(TrainCar selectedCar, bool getTotalPrice)
		{
			if (!getTotalPrice)
			{
				return GetUnitCatalogPrice(selectedCar);
			}

			List<TrainCar> trainSet = CarUtils.GetCCLTrainsetOrLocoAndTender(selectedCar);
			float totalTrainsetCatalogPrice = 0f;
			foreach (TrainCar car in trainSet)
			{
				totalTrainsetCatalogPrice += GetUnitCatalogPrice(car);
			}

			return totalTrainsetCatalogPrice;
		}

		private static float GetUnitCatalogPrice(TrainCar selectedCar)
		{
			TrainCarType_v2 carParentType = selectedCar.carLivery.parentType;
			return carParentType.damage.bodyPrice + carParentType.damage.wheelsPrice + carParentType.damage.electricalPowertrainPrice + carParentType.damage.mechanicalPowertrainPrice;
		}

		private static float GetPriceWhenBuyingWithLicense(TrainCar selectedCar, bool getTotalPrice)
		{
			float totalPrice;

			if (selectedCar.carLivery.requiredLicense == null) return 0;

			if (selectedCar.carType == TrainCarType.LocoShunter && selectedCar.carLivery.requiredLicense.price <= 0)
			{
				totalPrice = DE2_ARTIFICIAL_LICENSE_PRICE * Main.Settings.priceMultiplier;
			}
			else
			{
				totalPrice = selectedCar.carLivery.requiredLicense.price * Main.Settings.priceMultiplier;
			}

			if (getTotalPrice)
			{
				return totalPrice;
			}

			float numberOfCars = CarUtils.GetCCLTrainsetOrLocoAndTender(selectedCar).Count;

			return totalPrice / numberOfCars;
		}

		private static float GetTotalTrainsetSellPrice(TrainCar selectedCar, bool considerWear)
		{
			List<TrainCar> trainSet = CarUtils.GetCCLTrainsetOrLocoAndTender(selectedCar);

			float totalTrainsetSellPrice = 0f;
			foreach (TrainCar car in trainSet)
			{
				if (considerWear)
				{
					totalTrainsetSellPrice += GetUnitSellPriceWithWear(car);
				}
				else
				{
					totalTrainsetSellPrice += GetStoredPriceOrDefault(car);
				}
			}

			return totalTrainsetSellPrice;
		}

		private static float GetUnitSellPriceWithWear(TrainCar selectedCar)
		{
			float wearFactor = CalculateWearFactor(selectedCar.GetComponent<DamageController>());
			return GetStoredPriceOrDefault(selectedCar) * (0.75f - wearFactor);
		}

		private static float GetStoredPriceOrDefault(TrainCar car)
		{
			if (OwnedLocosManager.OwnedLocosLicensePrice.TryGetValue(car.CarGUID, out float storedPrice))
			{
				if (storedPrice > 0f) return storedPrice;
			}
			return 0f;
		}

		private static float CalculateWearFactor(DamageController carDmg)
		{
			if (carDmg == null)
			{
				return 0f;
			}
			
			float totalDmg = 1 - (carDmg.bodyDamage.currentHealth / carDmg.bodyDamage.maxHealth);

			int factorCount = 1;
			if (carDmg.wheels != null)
			{
				totalDmg += carDmg.wheels.DamagePercentage;
				factorCount++;
			}

			if (carDmg.mechanicalPT != null)
			{
				totalDmg += carDmg.mechanicalPT.DamagePercentage;
				factorCount++;
			}

			if (carDmg.electricalPT != null)
			{
				totalDmg += carDmg.electricalPT.DamagePercentage;
				factorCount++;
			}

			return Mathf.Clamp(0.5f * (totalDmg / factorCount), 0f, 0.5f);
		}
	}
}
