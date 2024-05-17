using System;
using System.Reflection;

using HarmonyLib;

using DV;

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
				__instance.display.SetContent("Cannot clear owned locomotives.");

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

	/*[HarmonyPatch(typeof(CommsRadioCarDeleter), nameof(CommsRadioCarDeleter.OnUse))]
	class CarDeleterPatch
	{
		static bool Prefix(CommsRadioCarDeleter __instance)
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

			return true;
		}
	}*/

	/*[HarmonyPatch(typeof(CommsRadioCarDeleter), nameof(CommsRadioCarDeleter.OnUpdate))]
	class CarDeleterPatch
	{
		private static readonly FieldInfo trainCarMaskField = typeof(CommsRadioCarDeleter).GetField("trainCarMask", BindingFlags.NonPublic | BindingFlags.Instance);
		private static readonly MethodInfo pointToCarMethod = typeof(CommsRadioCarDeleter).GetMethod("PointToCar", BindingFlags.NonPublic | BindingFlags.Instance);

		public static bool Prefix(CommsRadioCarDeleter __instance)
		{
			if (__instance.CurrentState == CommsRadioCarDeleter.State.ScanCarToDelete)
			{
				RaycastHit hit;
				LayerMask trainCarMask = (LayerMask)trainCarMaskField.GetValue(__instance);

				if (Physics.Raycast(__instance.signalOrigin.position, __instance.signalOrigin.forward, out hit, 100f, trainCarMask))
				{
					TrainCar trainCar2 = TrainCar.Resolve(hit.transform.root);
					if (trainCar2 != null && OwnedLocos.ownedLocos.ContainsKey(trainCar2.CarGUID))
					{
						// If the CarGUID is in the dictionary, call PointToCar(null)
						pointToCarMethod.Invoke(__instance, new object[] { null });
						return false;
					}
				}
			}

			return true;
		}
	}*/
}
