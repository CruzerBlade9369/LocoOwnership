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
		private const float MIN_TELE_PRICE = 2500f;

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
				if (getTotalTrainsetPrice)
				{
					return GetTotalTrainsetCatalogPrice(selectedCar);
				}

				TrainCarType_v2 carParentType = selectedCar.carLivery.parentType;
				return carParentType.damage.bodyPrice + carParentType.damage.wheelsPrice + carParentType.damage.electricalPowertrainPrice + carParentType.damage.mechanicalPowertrainPrice;
			}

			if (selectedCar.carType == TrainCarType.LocoShunter)
			{
				if (selectedCar.carLivery.requiredLicense.price > 0)
				{
					return selectedCar.carLivery.requiredLicense.price * Main.Settings.priceMultiplier;
				}
				else
				{
					return DE2_ARTIFICIAL_LICENSE_PRICE * Main.Settings.priceMultiplier;
				}
			}
			else
			{
				return selectedCar.carLivery.requiredLicense.price * Main.Settings.priceMultiplier;
			}
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

			// when using catalog price
			if (Main.Settings.locoCatPrices)
			{
				if (Main.Settings.advancedEco)
				{
					return GetTotalTrainsetSellPrice(selectedCar, considerWear: true);
				}

				return GetTotalTrainsetSellPrice(selectedCar, considerWear: false) / Main.Settings.priceMultiplier;
			}

			// old system replaced with calculation using purchase price dict
			// kinda makes no sense for selling price to be based on license price instead of purchase price eh

			if (Main.Settings.advancedEco)
			{
				return GetSellPriceWithWear(selectedCar);
			}

			return OwnedLocosManager.OwnedLocosLicensePrice[selectedCar.CarGUID] / Main.Settings.priceMultiplier;
		}

		public static float CalculateCarTeleportPrice(TrainCar selectedCar, EquiPointSet.Point? selectedPoint)
		{
			float carTeleportPrice;

			if (Main.Settings.freeCarTeleport)
			{
				return 0f;
			}

			Vector3 spawnPos = (Vector3)selectedPoint.Value.position + WorldMover.currentMove;
			float teleDistance = Vector3.Distance(selectedCar.transform.position, spawnPos) * 0.001f;

			carTeleportPrice = Mathf.RoundToInt(teleDistance * 200f) * 6;

			if (carTeleportPrice < MIN_TELE_PRICE)
			{
				return MIN_TELE_PRICE;
			}

			return carTeleportPrice;
		}

		private static float GetTotalTrainsetCatalogPrice(TrainCar selectedCar)
		{
			List<TrainCar> trainSet = CarUtils.GetCCLTrainsetOrLocoAndTender(selectedCar);

			float totalTrainsetCatalogPrice = 0f;
			foreach (TrainCar car in trainSet)
			{
				TrainCarType_v2 carParentType = car.carLivery.parentType;
				totalTrainsetCatalogPrice += carParentType.damage.bodyPrice + carParentType.damage.wheelsPrice + carParentType.damage.electricalPowertrainPrice + carParentType.damage.mechanicalPowertrainPrice;
			}

			return totalTrainsetCatalogPrice;
		}

		private static float GetTotalTrainsetSellPrice(TrainCar selectedCar, bool considerWear)
		{
			List<TrainCar> trainSet = CarUtils.GetCCLTrainsetOrLocoAndTender(selectedCar);

			float totalTrainsetSellPrice = 0f;
			foreach (TrainCar car in trainSet)
			{
				if (considerWear)
				{
					totalTrainsetSellPrice += GetSellPriceWithWear(car);
				}
				else
				{
					totalTrainsetSellPrice += OwnedLocosManager.OwnedLocosLicensePrice[car.CarGUID];
				}
			}

			return totalTrainsetSellPrice;
		}

		private static float GetSellPriceWithWear(TrainCar selectedCar)
		{
			float wearFactor = CalculateWearFactor(selectedCar.GetComponent<DamageController>());
			return OwnedLocosManager.OwnedLocosLicensePrice[selectedCar.CarGUID] * (0.75f - wearFactor);
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
