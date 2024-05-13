using HarmonyLib;
using Newtonsoft.Json.Linq;

using LocoOwnership.Shared;

namespace LocoOwnership.Patches
{
	[HarmonyPatch(typeof(SaveGameManager), nameof(SaveGameManager.Save))]
	class SaveOwnedCars
	{
		static void Prefix(SaveGameManager __instance)
		{
			JObject savedOwnedLocos = OwnedLocos.OnGameSaved();

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

			// Clear cache for new game load
			OwnedLocos.ClearCache();

			if (savedOwnedLocos != null)
			{
				Main.DebugLog("load savedownedlocos");
				OwnedLocos.OnGameLoad(savedOwnedLocos);
			}
		}
	}

	class OwnedLocosSaveData
	{
		public string guid;
		public string locoID;

		public OwnedLocosSaveData(string guid, string locoID)
		{
			this.guid = guid;
			this.locoID = locoID;
		}
	}
}
