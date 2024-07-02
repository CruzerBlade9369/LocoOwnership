using System.Reflection;
using HarmonyLib;

using DV;

using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.Patches
{
	[HarmonyPatch(typeof(CarVisitChecker))]
	public static class CarVisitCheckerPatch
	{
		[HarmonyPatch("IsRecentlyVisited", MethodType.Getter)]
		[HarmonyPrefix]
		public static bool IsRecentlyVisited_Prefix(CarVisitChecker __instance, ref bool __result)
		{
			FieldInfo carField = typeof(CarVisitChecker).GetField("car", BindingFlags.NonPublic | BindingFlags.Instance);
			TrainCar car = (TrainCar)carField.GetValue(__instance);

			if (OwnedLocos.ownedLocos.ContainsKey(car.CarGUID))
			{
				__result = true;
				return false;
			}

			return true;
		}
	}
}
