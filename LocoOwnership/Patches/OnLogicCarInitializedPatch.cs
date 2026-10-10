using DV.Damage;
using DV.PitStops;
using DV.ServicePenalty;
using DV.Simulation.Cars;
using DV.Utils;
using HarmonyLib;
using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.Patches
{
	[HarmonyPatch(typeof(SimController), nameof(SimController.OnLogicCarInitialized))]
	class OnLogicCarInitializedPatch
	{
		static bool Prefix(SimController __instance)
		{
			OwnedLocosManager.Instance.AssignOwnershipComponent(__instance.train);

			if (!OwnedLocosManager.Instance.IsLocoAlreadyOwned(__instance.train.CarGUID))
			{
				Main.DebugLog($"{__instance.train.ID} is not owned, using vanilla function");
				return true;
			}

			Main.DebugLog($"{__instance.train.ID} is owned, bypassing vanilla");

			__instance.train.LogicCarInitialized -= __instance.OnLogicCarInitialized;
			DamageController dmgController = __instance.GetComponent<DamageController>();

			__instance.gameObject.AddComponent<SimulatedCarPitStopParameters>().Initialize
				(
					__instance.resourceContainerController.resourceContainers,
					dmgController
				);

			__instance.debt = new SimulatedCarDebtTracker
				(
					dmgController,
					__instance.resourceContainerController,
					__instance.environmentDamageController,
					__instance.simFlow,
					__instance.train.ID,
					__instance.train.carType
				);

			SingletonBehaviour<OwnedCarsStateController>.Instance.RegisterCarStateTracker(__instance.train, __instance.debt);

			__instance.train.OnDestroyCar += __instance.OnCarDestroyed;

			return false;
		}
	}
}
