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
		public bool freeSandboxOwnership =
#if DEBUG
			true;
#else
			false;
#endif

		[Draw("Locomotives cost nothing at all")]
		public bool freeOwnership =
#if DEBUG
			true;
#else
			false;
#endif
		[Draw("Free locomotive requesting")]
		public bool freeCarTeleport =
#if DEBUG
			true;
#else
			false;
#endif

		[Draw("The funny")]
		public bool theFunny =
#if DEBUG
			true;
#else
			false;
#endif

		[Draw("Maximum number of owned locomotives", Min = 0, Max = 100)]
		public int maxLocosLimit = 16;

		public override void Save(UnityModManager.ModEntry entry)
		{
			Save<Settings>(this, entry);
		}

		public void OnChange() { }
	}
}
