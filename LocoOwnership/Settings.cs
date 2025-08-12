using LocoOwnership.OwnershipHandler;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace LocoOwnership
{
	public class Settings : UnityModManager.ModSettings, IDrawable
	{
		public readonly string? version = Main.mod?.Info.Version;

		[Draw("Enable logging")]
		public bool isLoggingEnabled =
#if DEBUG
			true;
#else
            false;
#endif

		[Draw("Locomotives cost nothing in sandbox")]
		public bool freeSandboxOwnership = false;

		[Draw("Locomotives cost nothing at all")]
		public bool freeOwnership = false;

		[Draw("Free locomotive requesting")]
		public bool freeCarTeleport = false;

		[Draw("Disable locomotive requesting")]
		public bool noLocoRequest = false;

		[Draw("Purchasing locomotives does not require demonstrator restoration")]
		public bool skipDemonstrator = false;

		[Draw("The funny (enable at your own risk)")]
		public bool theFunny = false;

		[Draw("Maximum number of owned locomotives", Min = 0, Max = 100)]
		public int maxLocosLimit = 16;

		[Draw("Locomotive buy/sell price multiplier (Does not apply when dynamic resell price is on)", Min = 2f, Max = 100f)]
		public float priceMultiplier = 2f;

		[Draw("Use locomotive catalog prices for purchase")]
		public bool locoCatPrices = true;

		[Draw("Dynamic resell price")]
		public bool advancedEco = true;

		public override void Save(UnityModManager.ModEntry entry)
		{
			Save(this, entry);
		}

		public void OnChange() { }

        public void DrawGUI(UnityModManager.ModEntry modEntry)
        {
            this.Draw(modEntry);
            DrawConfigs();
        }

		private void DrawConfigs()
		{
			GUILayout.BeginVertical(GUILayout.MinWidth(200), GUILayout.ExpandWidth(false));

			if (isLoggingEnabled)
			{
				GUILayout.Label("Debug functions");

				if (GUILayout.Button("Validate owned cars"))
				{
					OwnedLocosManager.ValidateOwnedCars();
				}

				if (GUILayout.Button("Print all owned cars data to console"))
				{
					if (OwnedLocosManager.OwnedLocos.Count <= 0 || OwnedLocosManager.OwnedLocosLicensePrice.Count <= 0)
					{
						Debug.Log("You don't have owned locos yet or you haven't loaded into a save!");
					}
					else
					{
						Debug.Log("Owned locos list:");
						foreach (KeyValuePair<string, string> kvp in OwnedLocosManager.OwnedLocos)
						{
							Debug.Log($"Guid = {kvp.Key}, LocoID = {kvp.Value}");
						}

						Debug.Log("Owned locos list, stored loco price:");
						foreach (KeyValuePair<string, float> kvp in OwnedLocosManager.OwnedLocosLicensePrice)
						{
							Debug.Log($"Guid = {kvp.Key}, stored loco price = {kvp.Value}");
						}

						Debug.Log("-----");
						Debug.Log($"Found {OwnedLocosManager.OwnedLocos.Count} locos, {OwnedLocosManager.CountLocosOnly()} without tender");
						Debug.Log($"Found {OwnedLocosManager.OwnedLocosLicensePrice.Count} loco price data");
					}
				}
			}

			GUILayout.EndVertical();
		}
	}
}
