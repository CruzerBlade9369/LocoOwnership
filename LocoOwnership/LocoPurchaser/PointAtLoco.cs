using CommsRadioAPI;
using DV;
using DV.Damage;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LocoOwnership.LocoPurchaser
{
	internal class PointAtLoco : PointAtSomething
	{
		public PointAtLoco(TrainCar selectedCar) : base(selectedCar)
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			Main.DebugLog("Loco purchasing should go here, currently unimplemented");
			return new PointAtNothing();
		}
	}
}
