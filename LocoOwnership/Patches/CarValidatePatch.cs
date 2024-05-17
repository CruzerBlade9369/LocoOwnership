using HarmonyLib;

using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.Patches
{

	[HarmonyPatch(typeof(AStartGameData), nameof(AStartGameData.DestroyAllInstances))]
	class CarValidatePatch
	{
		static void Prefix()
		{
			OwnedLocos.ValidateOwnedCars();
		}
	}
}
