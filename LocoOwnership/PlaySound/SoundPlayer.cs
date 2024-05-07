using System;

using DV;

using CommsRadioAPI;

namespace LocoOwnership.PlaySound
{
	internal class SoundPlayer : AStateBehaviour
	{
		private int selectedIndex;
		private static readonly string[] soundNames = {
		"Confirm", "Cancel", "Warning", "Switch", "ModeEnter", "HoverOver", "MoneyRemoved"
		};

		public SoundPlayer(int soundIndex)
			: base(new CommsRadioState(
				titleText: "Playsound",
				contentText: $"Play sound: {soundNames[soundIndex]}",
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			selectedIndex = soundIndex;
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			switch (action)
			{
				case InputAction.Activate:
					SoundSelector(selectedIndex, utility);
					return new SoundPlayer(selectedIndex);

				case InputAction.Up:
					selectedIndex = PreviousIndex();
					return new SoundPlayer(selectedIndex);

				case InputAction.Down:
					selectedIndex = NextIndex();
					return new SoundPlayer(selectedIndex);

				default:
					throw new Exception($"Unexpected action: {action}");
			};

			return this;
		}
		private int NextIndex()
		{
			int nextIndex = selectedIndex + 1;
			if (nextIndex >= 7)
			{
				nextIndex = 0;
			}
			return nextIndex;
		}

		private int PreviousIndex()
		{
			int previousIndex = selectedIndex - 1;
			if (previousIndex < 0)
			{
				previousIndex = 7 - 1;
			}
			return previousIndex;
		}

		public static void SoundSelector(int soundIndex, CommsRadioUtility utility)
		{
			switch (soundIndex)
			{
				case 0:
					utility.PlaySound(VanillaSoundCommsRadio.Confirm);
					break;

				case 1:
					utility.PlaySound(VanillaSoundCommsRadio.Cancel);
					break;

				case 2:
					utility.PlaySound(VanillaSoundCommsRadio.Warning);
					break;

				case 3:
					utility.PlaySound(VanillaSoundCommsRadio.Switch);
					break;

				case 4:
					utility.PlaySound(VanillaSoundCommsRadio.ModeEnter);
					break;

				case 5:
					utility.PlaySound(VanillaSoundCommsRadio.HoverOver);
					break;

				case 6:
					utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
					break;

				default:
					throw new Exception("This should not happen");
			}
		}
	}
}
