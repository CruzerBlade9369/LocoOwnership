using System;

using DV;
using DV.Localization;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.LocoPurchaser;
using LocoOwnership.LocoSeller;
using LocoOwnership.LocoRequester;
using System.Collections.Generic;

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

			if (!Main.settings.noLocoRequest)
			{
				items.Add(("lo/radio/general/request", "lo/radio/locorequest/content"));
			}

			return items;
		}

		private int menuIndex;

		public OwnershipMenus(int menuIndex = 0)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L(GetMenuItems()[menuIndex].title),
				contentText: LocalizationAPI.L(GetMenuItems()[menuIndex].content),
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.menuIndex = menuIndex;
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
							RequestLocoSelector.RefreshRequestableLocos();

							if (RequestLocoSelector.GetRequestableLocosCount() <= 0)
							{
								utility.PlaySound(VanillaSoundCommsRadio.Warning);
								return new RequestFail(1);
							}

							utility.PlaySound(VanillaSoundCommsRadio.ModeEnter);
							return new RequestLocoSelector();
						default:
							Debug.LogError("Ownership menu selector error");
							throw new Exception($"Unexpected index: {menuIndex}");
					}

				case InputAction.Up:
					return new OwnershipMenus(PreviousIndex());

				case InputAction.Down:
					return new OwnershipMenus(NextIndex());

				default:
					Debug.Log("Ownership menu error: why are you here?");
					throw new Exception($"Unexpected action: {action}");
			}
		}

		private int NextIndex()
		{
			int nextIndex = menuIndex + 1;
			if (nextIndex >= GetMenuItems().Count)
			{
				nextIndex = 0;
			}
			return nextIndex;
		}

		private int PreviousIndex()
		{
			int previousIndex = menuIndex - 1;
			if (previousIndex < 0)
			{
				previousIndex = GetMenuItems().Count - 1;
			}
			return previousIndex;
		}
	}
}
