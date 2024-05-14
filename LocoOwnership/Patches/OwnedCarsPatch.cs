using System.Reflection;

using HarmonyLib;

using DV.Simulation.Cars;
using DV.ServicePenalty;

using LocoOwnership.Shared;

namespace LocoOwnership.Patches
{
	/*[HarmonyPatch(typeof(SimController), "OnLogicCarInitialized")]
	class ObtainDebtValue
	{
		static void Postfix(SimController __instance)
		{
			FieldInfo debtField = typeof(SimController).GetField("debt", BindingFlags.NonPublic | BindingFlags.Instance);
			SimulatedCarDebtTracker debt = (SimulatedCarDebtTracker)debtField.GetValue(__instance);

			if (debt != null)
			{
				OwnedLocos.Debt = debt;
				Main.DebugLog($"Debt variable successfully stolen: {debt}");
			}
		}
	}*/
}
