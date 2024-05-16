using System;
using System.Reflection;

using HarmonyLib;

using DV;

using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.Patches
{
	[HarmonyPatch(typeof(CommsRadioCarDeleter), nameof(CommsRadioCarDeleter.OnUse))]
	class CarDeleterPatch
	{
		static bool Prefix(CommsRadioCarDeleter __instance)
		{
			var currentState = Traverse.Create(__instance).Property("CurrentState").GetValue<CommsRadioCarDeleter.State>();

			if (currentState == CommsRadioCarDeleter.State.ConfirmDelete)
			{
				TrainCar carToDelete = Traverse.Create(__instance).Field("carToDelete").GetValue<TrainCar>();

				if (carToDelete != null && OwnedLocos.ownedLocos.ContainsKey(carToDelete.CarGUID))
				{
					MethodInfo clearFlagsMethod = typeof(CommsRadioCarDeleter).GetMethod("ClearFlags", BindingFlags.NonPublic | BindingFlags.Instance);
					if (clearFlagsMethod != null)
					{
						clearFlagsMethod.Invoke(__instance, null);
					}

					MethodInfo setStateMethod = typeof(CommsRadioCarDeleter).GetMethod("SetState", BindingFlags.NonPublic | BindingFlags.Instance);
					if (setStateMethod != null)
					{
						var stateEnum = typeof(CommsRadioCarDeleter).GetNestedType(nameof(CommsRadioCarDeleter.State), BindingFlags.Public);
						var cancelDeleteState = Enum.Parse(stateEnum, nameof(CommsRadioCarDeleter.State.CancelDelete));
						setStateMethod.Invoke(__instance, new object[] { cancelDeleteState });
					}

					CommsRadioController.PlayAudioFromRadio(__instance.warningSound, __instance.transform);
					__instance.display.SetContent("Cannot clear owned locomotives.");

					return false;
				}
			}
			
			return true;
		}
	}
}
