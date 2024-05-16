using System.Reflection;

using HarmonyLib;

using UnityEngine;

using DV;

using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.Patches
{
	[HarmonyPatch(typeof(CommsRadioCarDeleter), nameof(CommsRadioCarDeleter.OnUpdate))]
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
	}
}
