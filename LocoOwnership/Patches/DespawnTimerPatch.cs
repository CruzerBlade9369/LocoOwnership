/*using HarmonyLib;

using LocoOwnership.OwnershipHandler;
using UnityEngine;

namespace LocoOwnership.Patches
{
	[HarmonyPatch(typeof(UnusedTrainCarDeleter), "AreDeleteConditionsFulfilled")]
	[HarmonyPriority(Priority.Last)]
	class OwnedLocoDespawnPatcher
	{
		[HarmonyAfter("PersistentJobsMod")]
		static bool Prefix(ref bool __result, TrainCar trainCar)
		{
			Debug.Log("locoownership: prefix to patch car delete conditions being called");
			if (OwnedLocos.ownedLocos.ContainsKey(trainCar.CarGUID))
			{
				__result = false;

				return false;
			}

			return true;
		}
	}
}*/
