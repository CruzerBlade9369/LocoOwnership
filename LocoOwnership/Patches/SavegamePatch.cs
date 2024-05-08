using System;
using System.Collections.Generic;

using HarmonyLib;
using Newtonsoft.Json.Linq;

using UnityEngine;

using DV;
using DV.JObjectExtstensions;
using DV.ThingTypes;



namespace LocoOwnership.Patches
{
	class SavegamePatch
	{
		public static void InitOwnership()
		{

		}

		public static JObject CreateLocoDict()
		{
			JObject ownedLocos = new();

			Main.DebugLog("starting foreach to generate lists");
			foreach (TrainCarType carTypes in Enum.GetValues(typeof(TrainCarType)))
			{
				// Skip NotSet (why the hell does it even exist)
				if (carTypes == TrainCarType.NotSet)
				{
					continue;
				}

				// Get all existing cars
				Main.DebugLog("get all cars");
				Main.DebugLog($"current car to get: {carTypes}");
				TrainCar carType = TrainCar.Resolve(TrainCar.GetCarPrefab(carTypes));

				if (carType is null)
				{
					Main.DebugLog("carType is null!!!");
					continue;
				}

				// Check if car is loco
				Main.DebugLog("checking if car is loco");
				if (carType.IsLoco)
				{
					Main.DebugLog("generate list");
					List<string> ownedLocosOfThisType = new List<string>();
					Main.DebugLog("assign list to jobject");
					ownedLocos[carType.ToString()] = JArray.FromObject(ownedLocosOfThisType);
				}
			}

			Main.DebugLog("returning ownedlocos");
			return ownedLocos;
		}

		// DEBUG FUNCTION, REMOVE BEFORE BUILD
		public static void PrintLocoDicts(JObject ownedLocos)
		{
			Main.DebugLog(ownedLocos.ToString());
		}
	}
}
