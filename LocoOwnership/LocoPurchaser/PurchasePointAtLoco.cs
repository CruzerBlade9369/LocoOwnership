using System;

using DV;
using DV.Damage;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;

using UnityEngine;

using CommsRadioAPI;

namespace LocoOwnership.LocoPurchaser
{
	// This class inherits point at something for the radio state
	internal class PurchasePointAtLoco : PurchasePointAtSomething
	{
		public PurchasePointAtLoco(TrainCar selectedCar) : base(selectedCar)
		{

		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			utility.PlaySound(VanillaSoundCommsRadio.Confirm);
			return new TransactionPurchaseConfirm(selectedCar);
		}
	}
}
