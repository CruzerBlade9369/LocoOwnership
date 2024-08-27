using DV.ThingTypes;

namespace LocoOwnership.Shared
{
	public class CarGetters
	{
		public static TrainCar GetTender(TrainCar selectedCar)
		{
			// Check if we're buying S282
			bool isSteamEngine = CarTypes.IsMUSteamLocomotive(selectedCar.carType);
			bool hasTender = selectedCar.rearCoupler.IsCoupled() && CarTypes.IsTender(selectedCar.rearCoupler.coupledTo.train.carLivery);

			TrainCar tender = null;

			// Get tender if S282
			if (isSteamEngine && hasTender)
			{
				tender = selectedCar.rearCoupler.coupledTo.train;
			}

			return tender;
		}
	}
}
