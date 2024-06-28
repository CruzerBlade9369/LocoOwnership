using HarmonyLib;

using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.Patches
{

	[HarmonyPatch(typeof(AStartGameData), nameof(AStartGameData.DestroyAllInstances))]
	class CarValidatePatch
	{
		static void Prefix()
		{
			Main.DebugLog("Beginning validating existence of owned cars");
			OwnedLocos.ValidateOwnedCars();
			Main.DebugLog("Beginning validating states of owned cars");
			OwnedLocos.OwnedCarsStatesValidate();
		}
	}
}
