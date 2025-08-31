using DV.ThingTypes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LocoOwnership.Shared
{
	public class CarUtils
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

		public static bool IsTrainsetValidForLoco(TrainCar car)
		{
			// add check for CCL trainset validation here, to be implemented

			if (CarTypes.IsMUSteamLocomotive(car.carType))
			{
				if (!car.rearCoupler.IsCoupled())
				{
					return false;
				}

				if (!CarTypes.IsTender(car.rearCoupler.coupledTo.train.carLivery))
				{
					return false;
				}
			}
			return true;
		}

		public static List<TrainCar> GetCCLTrainsetOrLocoAndTender(TrainCar car)
		{
			List<TrainCar> trainSet = GetCCLTrainset(car);
			if (trainSet.Count <= 0)
			{
				trainSet = GetLocoAndTenderIfAny(car);
			}

			return trainSet;
		}

		public static List<TrainCar> GetLocoAndTenderIfAny(TrainCar car)
		{
			List<TrainCar> trainSet = new List<TrainCar> { car };
			var tender = GetTender(car);
			if (tender != null)
			{
				trainSet.Add(tender);
			}

			return trainSet;
		}

		public static List<TrainCar> GetCCLTrainset(TrainCar car)
		{
			if (Main.IsCCLLoaded)
			{
				return GetTrainset(car);
			}

			return new List<TrainCar>();
		}

		private static List<TrainCar> GetTrainset(TrainCar car)
		{
			return CCL.Importer.CarManager.GetInstancedTrainset(car).ToList();
		}
	}
}
