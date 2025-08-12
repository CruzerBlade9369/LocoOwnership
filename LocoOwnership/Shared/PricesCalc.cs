using UnityEngine;

using DV.PointSet;
using DV.ThingTypes;
using DV.UserManagement;

using LocoOwnership.OwnershipHandler;
using DV.Damage;

namespace LocoOwnership.Shared
{
	public class PricesCalc
	{
		private const float DE2_ARTIFICIAL_LICENSE_PRICE = 10000f;
		private const float MIN_TELE_PRICE = 2500f;

		public static float CalculateBuyPrice(TrainCar selectedCar)
		{

			if (Main.settings.freeOwnership)
			{
				return 0f;
			}

			if (Main.settings.freeSandboxOwnership && UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
			{
				return 0f;
			}

			if (Main.settings.locoCatPrices)
			{
				TrainCarType_v2 carParentType = selectedCar.carLivery.parentType;
				return carParentType.damage.bodyPrice + carParentType.damage.wheelsPrice + carParentType.damage.electricalPowertrainPrice + carParentType.damage.mechanicalPowertrainPrice;
			}

			if (selectedCar.carType == TrainCarType.LocoShunter)
			{
				if (selectedCar.carLivery.requiredLicense.price > 0)
				{
					return selectedCar.carLivery.requiredLicense.price * Main.settings.priceMultiplier;
				}
				else
				{
					return DE2_ARTIFICIAL_LICENSE_PRICE * Main.settings.priceMultiplier;
				}
			}
			else
			{
				return selectedCar.carLivery.requiredLicense.price * Main.settings.priceMultiplier;
			}
		}

		public static float CalculateSellPrice(TrainCar selectedCar)
		{
			if (Main.settings.freeOwnership)
			{
				return 0f;
			}

			if (Main.settings.freeSandboxOwnership && UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
			{
				return 0f;
			}

			if (Main.settings.advancedEco)
			{
				float wearFactor = CalculateWearFactor(selectedCar.GetComponent<DamageController>());
				return OwnedLocosManager.OwnedLocosLicensePrice[selectedCar.CarGUID] * (0.75f - wearFactor);
			}

			// old system replaced with calculation using purchase price dict
			// kinda makes no sense for selling price to be based on license price instead of purchase price eh

			return OwnedLocosManager.OwnedLocosLicensePrice[selectedCar.CarGUID] / Main.settings.priceMultiplier;
		}

		public static float CalculateCarTeleportPrice(TrainCar selectedCar, EquiPointSet.Point? selectedPoint)
		{
			float carTeleportPrice;

			if (Main.settings.freeCarTeleport)
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
