using HarmonyLib;
using Newtonsoft.Json.Linq;
using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.Patches
{
	[HarmonyPatch(typeof(SaveGameManager), nameof(SaveGameManager.Save))]
	class SaveOwnedCars
	{
		static void Prefix(SaveGameManager __instance)
		{
			JObject savedOwnedLocos = OwnedLocosManager.Instance.OnGameSaved();

			SaveGameManager.Instance.data.SetJObject("MOD_LOCOOWNERSHIP", savedOwnedLocos);
		}
	}

	[HarmonyPatch(typeof(CarsSaveManager), nameof(CarsSaveManager.Load))]
	class LoadOwnedCars
	{
		static void Prefix(JObject savedData)
		{
			if (savedData == null)
			{
				Main.DebugLog("savedData is null, nothing is loaded!");
				return;
			}

			JObject savedOwnedLocos = SaveGameManager.Instance.data.GetJObject("MOD_LOCOOWNERSHIP");

			OwnedLocosManager.Instance.ClearTracker();

			if (savedOwnedLocos != null)
			{
				OwnedLocosManager.Instance.OnGameLoad(savedOwnedLocos);
			}
		}
	}
}
