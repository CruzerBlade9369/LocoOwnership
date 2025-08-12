using HarmonyLib;

using DV;

using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.Patches
{
	[HarmonyPatch(typeof(CarVisitChecker), nameof(CarVisitChecker.IsRecentlyVisited), MethodType.Getter)]
	class CarVisitCheckerPatch
	{
		static bool Prefix(CarVisitChecker __instance, ref bool __result)
		{
			if (OwnedLocosManager.HasLocoGUIDAsKey(__instance.car.CarGUID))
			{
				__result = true;
				return false;
			}

			return true;
		}
	}
}
