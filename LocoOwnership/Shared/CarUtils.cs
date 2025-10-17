using DV.ThingTypes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LocoOwnership.Shared
{
	public class CarUtils
	{
		public static TrainCar GetTender(TrainCar selectedCar)
		{
			bool isSteamEngine = CarTypes.IsMUSteamLocomotive(selectedCar.carType);
			bool hasTender = selectedCar.rearCoupler.IsCoupled() && CarTypes.IsTender(selectedCar.rearCoupler.coupledTo.train.carLivery);

			TrainCar tender = null;

			if (isSteamEngine && hasTender)
			{
				tender = selectedCar.rearCoupler.coupledTo.train;
			}

			return tender;
		}

		public static bool IsAnyLocoInSetDerailed(TrainCar car)
		{
			List<TrainCar> trainSet = GetCCLTrainsetOrLocoAndTender(car);

			foreach (TrainCar trainCar in trainSet)
			{
				if (trainCar.derailed) return true;
			}

			return false;
		}

		public static bool IsLocoOrLocosetValid(TrainCar car)
		{
			if (CarTypes.IsMUSteamLocomotive(car.carType))
			{
				if (!car.rearCoupler.IsCoupled())
					return false;

				if (!CarTypes.IsTender(car.rearCoupler.coupledTo.train.carLivery))
					return false;
			}

			if (!Main.IsCCLLoaded) return true;

			return CheckCCLTypeWrapper(car);
		}

		private static bool CheckCCLTypeWrapper(TrainCar car)
		{
			if (car.carLivery is CCL.Importer.Types.CCL_CarVariant)
			{
				return IsCCLLocosetValid(car);
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

		public static bool IsCCLLocosetValid(TrainCar car)
		{
			if (Main.IsCCLLoaded)
			{
				return IsLocosetValid(car);
			}

			return true;
		}

		private static bool IsLocosetValid(TrainCar car)
		{
			if (CCL.Importer.CarManager.TryGetInstancedTrainset(car, out var set)
				is CCL.Importer.CarManager.TrainSetCompleteness.NotPartOfTrainset)
				return true;

			if (set.Length > 0)
				return true;

			return false;
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
