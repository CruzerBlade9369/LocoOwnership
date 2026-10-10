using DV.Interaction;
using HarmonyLib;
using LocoOwnership.OwnershipHandler;
using static DV.Interaction.PaintCan;

namespace LocoOwnership.Patches
{
	[HarmonyPatch(typeof(PaintCan), (nameof(PaintCan.CheckPaintApplicationValidity)))]
	class PaintCanPatch
	{
		static void Postfix(TrainCar target, ref Validity __result)
		{
			if (target.TryGetComponent<LocoOwnershipController>(out _) && __result == Validity.NotOwnedLoco)
			{
				__result = Validity.Ok;
			}
		}
	}
}
