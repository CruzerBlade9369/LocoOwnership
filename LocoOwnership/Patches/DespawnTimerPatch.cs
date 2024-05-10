using HarmonyLib;

using LocoOwnership.Shared;

namespace LocoOwnership.Patches
{
	[HarmonyPatch(typeof(UnusedTrainCarDeleter), "AreDeleteConditionsFulfilled")]
	class OwnedLocoDespawnPatcher
	{
		static bool Prefix(ref bool __result,  TrainCar trainCar)
		{
			if (OwnedLocos.ownedLocos.ContainsKey(trainCar.CarGUID))
			{
				__result = false;

				return false;
			}

			return true;
		}
	}
}
