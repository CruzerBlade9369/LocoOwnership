using System;
using System.Reflection;

using HarmonyLib;
using UnityModManagerNet;

using DV;

using UnityEngine;

using CommsRadioAPI;
using DVLangHelper.Runtime;

using LocoOwnership.Menus;

namespace LocoOwnership
{
	[EnableReloading]
	public static class Main
	{
		public static bool enabled;
		public static UnityModManager.ModEntry? mod;
		public static Settings settings = new Settings();
		public static CommsRadioMode CommsRadioMode { get; private set; }

		private static bool Load(UnityModManager.ModEntry modEntry)
		{
			Harmony? harmony = null;

			try
			{
				harmony = new Harmony(modEntry.Info.Id);
				harmony.PatchAll(Assembly.GetExecutingAssembly());
				DebugLog("Attempting patch.");

				mod = modEntry;
				modEntry.OnGUI = OnGui;
				modEntry.OnSaveGUI = OnSaveGui;

				var translations = new TranslationInjector("cruzer.locoownership");
				string localizationUrl = "https://docs.google.com/spreadsheets/d/1UyoJuIiUykiHizaiM1qkxH4ji-em7NU-4MTgiHIyJeE/export?format=csv";
				translations.AddTranslationsFromWebCsv(localizationUrl);

				ControllerAPI.Ready += StartCommsRadio;
			}
			catch (Exception ex)
			{
				modEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
				harmony?.UnpatchAll(modEntry.Info.Id);
				return false;
			}

			return true;
		}

		static void OnGui(UnityModManager.ModEntry modEntry)
		{
			settings.Draw(modEntry);
		}

		static void OnSaveGui(UnityModManager.ModEntry modEntry)
		{
			settings.Save(modEntry);
		}

		public static void DebugLog(string message)
		{
			if (settings.isLoggingEnabled)
				mod?.Logger.Log(message);
		}

		public static void StartCommsRadio()
		{
			CommsRadioMode = CommsRadioMode.Create(new MainMenu(), Color.blue, (mode) => mode is CommsRadioCrewVehicle);
		}
	}
}
