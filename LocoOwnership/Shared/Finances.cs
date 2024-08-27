using UnityEngine;

using DV.PointSet;
using DV.ThingTypes;
using DV.UserManagement;

namespace LocoOwnership.Shared
{
	public class Finances
	{
		public const float DE2_ARTIFICIAL_LICENSE_PRICE = 10000f;
		public const float MIN_TELE_PRICE = 2500f;

		public float CalculateBuyPrice(TrainCar selectedCar)
		{
			float carBuyPrice;

			if (Main.settings.freeOwnership)
			{
				return 0f;
			}

			if (Main.settings.freeSandboxOwnership && UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
			{
				return 0f;
			}

			if (selectedCar.carType == TrainCarType.LocoShunter)
			{
				carBuyPrice = DE2_ARTIFICIAL_LICENSE_PRICE * 2f;
			}
			else
			{
				carBuyPrice = selectedCar.carLivery.requiredLicense.price * 2f;
			}

			return carBuyPrice;
		}

		public float CalculateSellPrice(TrainCar selectedCar)
		{
			float carSellPrice;

			if (Main.settings.freeOwnership)
			{
				return 0f;
			}

			if (Main.settings.freeSandboxOwnership && UserManager.Instance.CurrentUser.CurrentSession.GameMode.Equals("FreeRoam"))
			{
				return 0f;
			}

			if (selectedCar.carType == TrainCarType.LocoShunter)
			{
				carSellPrice = DE2_ARTIFICIAL_LICENSE_PRICE / 2f;
			}
			else
			{
				carSellPrice = selectedCar.carLivery.requiredLicense.price / 2f;
			}

			return carSellPrice;
		}

		public float CalculatCarTeleportPrice(TrainCar selectedCar, EquiPointSet.Point? selectedPoint)
		{
			float carTeleportPrice;

			if (Main.settings.freeCarTeleport)
			{
				return 0f;
			}

			Vector3 spawnPos = (Vector3)selectedPoint.Value.position + WorldMover.currentMove;
			float teleDistance = Vector3.Distance(selectedCar.transform.position, spawnPos) * 0.001f;

			carTeleportPrice = Mathf.RoundToInt(teleDistance * 150f) * 6;

			if (carTeleportPrice < MIN_TELE_PRICE)
			{
				return MIN_TELE_PRICE;
			}

			return carTeleportPrice;
		}
	}
}
