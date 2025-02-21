using System.Reflection;

using HarmonyLib;

using DV;

using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.Patches
{
	[HarmonyPatch(typeof(CarVisitChecker), "IsRecentlyVisited", MethodType.Getter)]
	public static class CarVisitCheckerPatch
	{
		static bool Prefix(CarVisitChecker __instance, ref bool __result)
		{
			FieldInfo carField = typeof(CarVisitChecker).GetField("car", BindingFlags.NonPublic | BindingFlags.Instance);
			TrainCar car = (TrainCar)carField.GetValue(__instance);

			if (OwnedLocos.HasLocoGUIDAsKey(car.CarGUID))
			{
				__result = true;
				return false;
			}

			return true;
		}
	}
}
