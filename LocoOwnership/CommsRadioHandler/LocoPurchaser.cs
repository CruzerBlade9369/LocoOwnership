using DV;
using CommsRadioAPI;
using UnityEngine;

namespace LocoOwnership.CommsRadioHandler
{
	internal static class LocoPurchaserMode
	{
		public static void Create()
		{
			CommsRadioMode.Create(new LocoPurchaser.PurchaseMenu(), new Color(1f, 0f, 0.9f, 1f), (mode) => mode is CommsRadioCarDeleter);
		}
	}
}

