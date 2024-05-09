using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using UnityEngine;

using DV;
using DV.JObjectExtstensions;
using DV.ThingTypes;

namespace LocoOwnership.Shared
{
	internal class OwnedLocos
	{
		private const int MAX_OWNED_LOCOS = 16;

		// This is the cache
		private static Dictionary<string, string> ownedLocos = new Dictionary<string, string>();

		public static void ClearCache()
		{
			Main.DebugLog("Clearing owned loco list cache.");
			ownedLocos.Clear();
		}

		public bool OnLocoBuy(TrainCar selectedCar)
		{
			bool cachingSuccess = false;

			// Check if player already has enough owned locos
			if (ownedLocos.Count >= MAX_OWNED_LOCOS)
			{
				return cachingSuccess;
			}

			string guid = selectedCar.CarGUID;
			string locoID = selectedCar.ID;

			if (ownedLocos.ContainsKey(guid))
			{
				throw new Exception("Loco GUID duplicate!");
			}
			else
			{
				ownedLocos.Add(guid, locoID);

				foreach (KeyValuePair<string, string> kvp in ownedLocos)
				{
					Main.DebugLog($"Key = {kvp.Key}, Value = {kvp.Value}");
				}

				cachingSuccess = true;
				return cachingSuccess;
			}
		}

		public void OnLocoSell(TrainCar selectedCar)
		{
			string guid = selectedCar.CarGUID;

			if (ownedLocos.ContainsKey(guid))
			{
				ownedLocos.Remove(guid);
			}
			else
			{
				throw new Exception("Loco GUID not found!");
			}
		}

		// Convert JObject of owned locos back into dict and apply to cache
		public static void OnGameLoad(JObject savedOwnedLocos)
		{
			JObject[] jobjectArray = savedOwnedLocos.GetJObjectArray("savedOwnedLocos");

			if (jobjectArray != null)
			{
				foreach (JObject jobject in jobjectArray)
				{
					var guid = jobject.GetString("guid");
					var name = jobject.GetString("locoType");

					if (!ownedLocos.ContainsKey(guid))
					{
						ownedLocos.Add(guid, name);
					}
				}
			}
		}

		// Convert owned locos list cache into JObjects for savegame
		public static JObject OnGameSaved()
		{
			JObject savedOwnedLocos = new();

			JObject[] array = new JObject[ownedLocos.Count];

			int i = 0;

			foreach (var kvp in ownedLocos)
			{
				JObject dataObject = new JObject();

				dataObject.SetString("guid", kvp.Key);
				dataObject.SetString("locoType", kvp.Value);

				array[i] = dataObject;

				i++;
			}

			savedOwnedLocos.SetJObjectArray("savedOwnedLocos", array);

			return savedOwnedLocos;
		}
	}
}
