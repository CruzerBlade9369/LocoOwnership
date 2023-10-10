using UnityModManagerNet;
using HarmonyLib;
using System.Collections.Generic;
using DV;

namespace LocoOwnership
{
	public class CommsRadio
	{
		public static CommsRadioController? Controller => controller;
		private static CommsRadioController? controller;

		[HarmonyPatch(typeof(CommsRadioController), "Awake")]
		class CommsRadioControllerAwakePatch
		{
			public static CommsRadioLocoPurchaser? locoPurchaser = null;

			static void Postfix(CommsRadioController __instance, List<ICommsRadioMode> __allModes)
			{
				controller = __instance;

				if (locoPurchaser == null)
				{
					// NOT YET IMPLEMENTED
				}
			}
		}
	}
}
