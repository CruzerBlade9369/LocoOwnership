using System;
using DV;
using DV.Localization;
using UnityEngine;
using CommsRadioAPI;
using LocoOwnership.LocoPurchaser;
using LocoOwnership.LocoSeller;
using LocoOwnership.LocoRequester;
using System.Collections.Generic;
using LocoOwnership.OwnershipHandler;

namespace LocoOwnership.Menus
{
	public class OwnershipMenus : AStateBehaviour
	{
		private static List<(string title, string content)> GetMenuItems()
		{
			var items = new List<(string, string)>
			{
				("lo/radio/general/purchase", "lo/radio/locopurchase/content"),
				("lo/radio/general/sell", "lo/radio/locosell/content")
			};

			if (!Main.Settings.noLocoRequest)
			{
				items.Add(("lo/radio/general/request", "lo/radio/locorequest/content"));
			}

			return items;
		}

		private static int menuIndex = 0;

		public OwnershipMenus()
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L(GetMenuItems()[menuIndex].title),
				contentText: LocalizationAPI.L(GetMenuItems()[menuIndex].content),
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			switch (action)
			{
				case InputAction.Activate:
					switch(menuIndex)
					{
						case 0:
							return new PurchasePointAtNothing();

						case 1:
							return new SellPointAtNothing();

						case 2:
							if (OwnedLocosManager.Instance.OwnedLocosTrackers.Count <= 0)
							{
								utility.PlaySound(VanillaSoundCommsRadio.Warning);
								return new RequestFail(1);
							}

							utility.PlaySound(VanillaSoundCommsRadio.ModeEnter);
							RequestLocoSelector.ValidateIndex();
							return new RequestLocoSelector();

						default:
							Debug.LogError("Ownership menu selector error");
							throw new Exception($"Unexpected index: {menuIndex}");
					}

				case InputAction.Up:
					PreviousIndex();
					return new OwnershipMenus();

				case InputAction.Down:
					NextIndex();
					return new OwnershipMenus();

				default:
					Debug.Log("Ownership menu error: why are you here?");
					throw new Exception($"Unexpected action: {action}");
			}
		}

		private void NextIndex()
		{
			int nextIndex = menuIndex + 1;
			if (nextIndex >= GetMenuItems().Count)
			{
				nextIndex = 0;
			}
			menuIndex = nextIndex;
		}

		private void PreviousIndex()
		{
			int previousIndex = menuIndex - 1;
			if (previousIndex < 0)
			{
				previousIndex = GetMenuItems().Count - 1;
			}
			menuIndex = previousIndex;
		}
	}
}
