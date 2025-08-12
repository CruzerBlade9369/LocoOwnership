using DV.Localization;
using DV.ServicePenalty;
using DV.ThingTypes;
using LocoOwnership.OwnershipHandler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LocoOwnership.Shared
{
	public class CarGetters
	{
		public static Dictionary<string, string> requestableOwnedLocos = new();

		public static TrainCar GetTender(TrainCar selectedCar)
		{
			// check if we're buying S282
			bool isSteamEngine = CarTypes.IsMUSteamLocomotive(selectedCar.carType);
			bool hasTender = selectedCar.rearCoupler.IsCoupled() && CarTypes.IsTender(selectedCar.rearCoupler.coupledTo.train.carLivery);

			TrainCar tender = null;

			// get tender if S282
			if (isSteamEngine && hasTender)
			{
				tender = selectedCar.rearCoupler.coupledTo.train;
			}

			return tender;
		}

		public static TrainCar TrainCarFromIndex(int index)
		{
			OwnedCarsStateController ocsc = OwnedCarsStateController.Instance;
			TrainCar loco = null;

			string ownedLocoGuid = requestableOwnedLocos.Keys.ElementAt(index);

			foreach (ExistingOwnedCarDebt eocd in ocsc.existingOwnedCarStates)
			{
				if (eocd.car.CarGUID == ownedLocoGuid && eocd.car.IsLoco)
				{
					loco = eocd.car;
					break;
				}
			}

			if (loco == null)
			{
				throw new Exception("content from index selector: traincar is null!!");
			}

			return loco;
		}

		public static void RefreshRequestableLocos()
		{
			requestableOwnedLocos.Clear();

			OwnedCarsStateController ocsc = OwnedCarsStateController.Instance;

			foreach (ExistingOwnedCarDebt eocd in ocsc.existingOwnedCarStates)
			{
				if (OwnedLocosManager.HasLocoGUIDAsKey(eocd.car.CarGUID) && eocd.car.IsLoco)
				{
					requestableOwnedLocos.Add(eocd.car.CarGUID, $"{LocalizationAPI.L(eocd.car.carLivery.localizationKey)} {eocd.car.ID}");
				}
			}

			var sortedDictionary = requestableOwnedLocos.OrderBy(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
			requestableOwnedLocos = sortedDictionary;
		}
	}
}
