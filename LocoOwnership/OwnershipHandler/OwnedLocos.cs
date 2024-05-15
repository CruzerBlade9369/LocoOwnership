using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using UnityEngine;

using DV.JObjectExtstensions;

namespace LocoOwnership.OwnershipHandler
{
	internal class OwnedLocos : MonoBehaviour
	{
		private const int MAX_OWNED_LOCOS = 16;

		DebtHandling debtHandling = new();

		public class DebtHandlingResult
		{
			public bool MaxOwnedLoc { get; set; }
			public bool DebtNotZero { get; set; }
			public bool Success { get; set; }
		}

		// This is the cache
		public static Dictionary<string, string> ownedLocos = new Dictionary<string, string>();

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region CACHE HANDLER

		public static void ClearCache()
		{
			Main.DebugLog("Clearing owned loco list cache.");
			ownedLocos.Clear();
		}

		public DebtHandlingResult OnLocoBuy(TrainCar selectedCar)
		{
			var result = new DebtHandlingResult();

			// Check if player already has enough owned locos
			if (ownedLocos.Count >= MAX_OWNED_LOCOS)
			{
				result.MaxOwnedLoc = true;
				return result;
			}

			string guid = selectedCar.CarGUID;
			string locoID = $"{selectedCar.carType}";

			if (ownedLocos.ContainsKey(guid))
			{
				throw new Exception("Loco GUID duplicate!");
			}
			else
			{
				bool allowOwnVehicle = debtHandling.SetVehicleToOwned(selectedCar);
				if (!allowOwnVehicle)
				{
					result.DebtNotZero = true;
					return result;
				}

				ownedLocos.Add(guid, locoID);

				foreach (KeyValuePair<string, string> kvp in ownedLocos)
				{
					Main.DebugLog($"Key = {kvp.Key}, Value = {kvp.Value}");
				}

				result.Success = true;
				return result;
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

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region LOAD/SAVE HANDLER

		// Convert JObject of owned locos back into dict and apply to cache
		public static void OnGameLoad(JObject savedOwnedLocos)
		{
			JObject[] jobjectArray = savedOwnedLocos.GetJObjectArray("savedOwnedLocos");

			if (jobjectArray != null)
			{
				foreach (JObject jobject in jobjectArray)
				{
					var guid = jobject.GetString("guid");
					var locoID = jobject.GetString("locoID");

					if (!ownedLocos.ContainsKey(guid))
					{
						ownedLocos.Add(guid, locoID);
					}
				}
			}
		}

		// Convert owned locos dict cache into JObjects for savegame
		public static JObject OnGameSaved()
		{
			JObject savedOwnedLocos = new();

			JObject[] array = new JObject[ownedLocos.Count];

			int i = 0;

			foreach (var kvp in ownedLocos)
			{
				JObject dataObject = new JObject();

				dataObject.SetString("guid", kvp.Key);
				dataObject.SetString("locoID", kvp.Value);

				array[i] = dataObject;

				i++;
			}

			savedOwnedLocos.SetJObjectArray("savedOwnedLocos", array);

			return savedOwnedLocos;
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/
	}
}
