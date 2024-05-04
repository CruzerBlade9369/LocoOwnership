using System;

using DV;
using DV.Damage;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;

using UnityEngine;

using CommsRadioAPI;

namespace LocoOwnership.LocoSeller
{
	internal class SellPointAtLoco : SellPointAtSomething
	{
		public SellPointAtLoco(TrainCar selectedCar) : base(selectedCar)
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			utility.PlaySound(VanillaSoundCommsRadio.Confirm);

			Main.DebugLog("Loco purchasing should go here, currently unimplemented");
			return new SellPointAtNothing();
		}
	}
}
