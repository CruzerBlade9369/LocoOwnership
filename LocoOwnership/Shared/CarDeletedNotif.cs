using System.Collections;
using DV;
using DV.UI;
using DV.UIFramework;
using RSG;

namespace LocoOwnership.Shared
{
	public static class CarDeletedNotif
	{
		// github.com/fauxnik/dv-message-box/blob/main/MessageBox/PopupAPI.cs
		// only need the OK popup and its only used in one specific edge case, no point in adding MessageBox as dependency

		public static IPromise<PopupResult> ShowOK(string msg, string? title = null, string? positive = null)
		{
			return new Promise<PopupResult>((resolve, reject) => {
				ShowOK(msg, (result) => resolve(result), title, positive);
			});
		}
		
		public static void ShowOK(string msg, PopupClosedDelegate onClose, string? title = "", string? positive = "Ok")
		{
			ShowPopup(uiReferences.popupOk, new PopupLocalizationKeys
			{
				titleKey = title,
				labelKey = msg,
				positiveKey = positive
			}, onClose);
		}

		private static void ShowPopup(Popup prefab, PopupLocalizationKeys keys, PopupClosedDelegate? onClose)
		{
			if (WorldStreamingInit.IsLoaded)
			{
				CoroutineManager.Instance.Run(Coro(prefab, keys, onClose));
			}
			else
			{
				WorldStreamingInit.LoadingFinished += () => CoroutineManager.Instance.Run(Coro(prefab, keys, onClose));
			}
		}

		private static IEnumerator Coro(Popup prefab, PopupLocalizationKeys locKeys, PopupClosedDelegate? onClose)
		{
			while (AppUtil.Instance.IsTimePaused)
				yield return null;
			while (!PopupManager.CanShowPopup())
				yield return null;
			Popup popup = PopupManager.ShowPopup(prefab, locKeys, keepLiteralData: true);
			if (onClose != null) { popup.Closed += onClose; }
		}

		private static PopupManager PopupManager
		{
			get => ACanvasController<CanvasController.ElementType>.Instance.PopupManager;
		}

		private static PopupNotificationReferences uiReferences
		{
			get => ACanvasController<CanvasController.ElementType>.Instance.uiReferences;
		}
	}
}
