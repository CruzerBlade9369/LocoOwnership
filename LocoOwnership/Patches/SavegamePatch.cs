using HarmonyLib;
using Newtonsoft.Json.Linq;

using UnityEngine;

using LocoOwnership.OwnershipHandler;

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

			// clear cache for new game load (here for redundancy)
			OwnedLocos.ClearCache();

			if (savedOwnedLocos != null)
			{
				OwnedLocos.OnGameLoad(savedOwnedLocos);
			}
		}
	}

	[HarmonyPatch(typeof(AStartGameData), MethodType.Constructor)]
	class CacheResetter
	{
		static void Prefix()
		{
			// clear cache when attempting to load a save
			OwnedLocos.ClearCache();
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
