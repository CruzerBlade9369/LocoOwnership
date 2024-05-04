using System;

using DV;
using DV.Damage;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;

using UnityEngine;

using CommsRadioAPI;

namespace LocoOwnership.LocoPurchaser
{
	internal class TransactionPurchase : TransactionPurchaseCommsState
	{
		public TransactionPurchase(TrainCar selectedCar) : base(selectedCar)
		{
			
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				throw new ArgumentException();
			}

			utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
			return new PurchasePointAtNothing();
		}
	}
}
