using System;
using System.Reflection;

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
			TrainCar carToDelete = Traverse.Create(__instance).Field("carToDelete").GetValue<TrainCar>();

			if (carToDelete != null && OwnedLocos.ownedLocos.ContainsKey(carToDelete.CarGUID))
			{
				__instance.display.SetContent(LocalizationAPI.L("lo/misc/cardeleterpatch"));

				MethodInfo setStateMethod = typeof(CommsRadioCarDeleter).GetMethod("SetState", BindingFlags.NonPublic | BindingFlags.Instance);
				if (setStateMethod != null)
				{
					var stateEnum = typeof(CommsRadioCarDeleter).GetNestedType(nameof(CommsRadioCarDeleter.State), BindingFlags.Public);
					var cancelDeleteState = Enum.Parse(stateEnum, nameof(CommsRadioCarDeleter.State.CancelDelete));
					setStateMethod.Invoke(__instance, new object[] { cancelDeleteState });
				}

				return false;
			}

			return true;
		}
	}
}
