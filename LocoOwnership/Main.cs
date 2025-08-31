using CommsRadioAPI;
using DV;
using DVLangHelper.Runtime;
using HarmonyLib;
using LocoOwnership.Menus;
using LocoOwnership.OwnershipHandler;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace LocoOwnership
{
	public static class Main
	{
		public static UnityModManager.ModEntry? mod;

		public static Settings Settings { get; private set; }
		public static CommsRadioMode CommsRadioMode { get; private set; }

		public static bool IsCCLLoaded { get; private set; }

		private static bool Load(UnityModManager.ModEntry modEntry)
		{
			Harmony? harmony = null;

			try
			{
				try
				{
					Settings = Settings.Load<Settings>(modEntry);
				}
				catch
				{
					Debug.LogWarning("Unabled to load mod settings. Using defaults instead.");
					Settings = new Settings();
				}
				mod = modEntry;

				harmony = new Harmony(modEntry.Info.Id);
				harmony.PatchAll(Assembly.GetExecutingAssembly());
				DebugLog("Attempting patch.");

				modEntry.OnGUI = Settings.DrawGUI;
				modEntry.OnSaveGUI = Settings.Save;

				var translations = new TranslationInjector("cruzer.locoownership");
				string localizationUrl = "https://docs.google.com/spreadsheets/d/1UyoJuIiUykiHizaiM1qkxH4ji-em7NU-4MTgiHIyJeE/export?format=csv";
				translations.AddTranslationsFromWebCsv(localizationUrl);

				// detect if ccl is loaded
				var ccl = UnityModManager.modEntries.FirstOrDefault(mod => mod.Info.Id == "DVCustomCarLoader");
				if (ccl != null && ccl.Active)
				{
					IsCCLLoaded = true;
				}
				Debug.Log($"Loco Ownership CCL integration: CCL is loaded? {IsCCLLoaded}");

				ControllerAPI.Ready += StartCommsRadio;
				OwnedLocosManager.Initialize();
			}
			catch (Exception ex)
			{
				modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
				harmony?.UnpatchAll(modEntry.Info.Id);
				return false;
			}

			return true;
		}

		public static void DebugLog(string message)
		{
			if (Settings.isLoggingEnabled)
				mod?.Logger.Log(message);
		}

		public static void StartCommsRadio()
		{
			CommsRadioMode = CommsRadioMode.Create(new MainMenu(), Color.blue, (mode) => mode is CommsRadioCrewVehicle);
		}
	}
}
