using HarmonyLib;

using DV;
using DV.Localization;

using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.Patches
{
	[HarmonyPatch(typeof(CommsRadioCarDeleter), nameof(CommsRadioCarDeleter.OnUpdate))]
	class CarDeleterPatch
	{
		static bool Prefix(CommsRadioCarDeleter __instance)
		{
			if (__instance.carToDelete != null && OwnedLocosManager.HasLocoGUIDAsKey(__instance.carToDelete.CarGUID))
			{
				__instance.display.SetContent(LocalizationAPI.L("lo/misc/cardeleterpatch"));
				__instance.SetState(CommsRadioCarDeleter.State.CancelDelete);

				return false;
			}

			return true;
		}
	}
}
