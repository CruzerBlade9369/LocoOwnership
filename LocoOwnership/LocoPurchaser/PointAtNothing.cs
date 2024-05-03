using CommsRadioAPI;
using DV;
using DV.Simulation.Cars;
using DV.Simulation.Controllers;
using DV.Damage;
using DV.ThingTypes;
using UnityEngine;
using System;

namespace LocoOwnership.LocoPurchaser
{
	internal class PointAtNothing : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 100f;

		private TrainCar selectedCar;
		private Transform signalOrigin;
		private int trainCarMask;

		public PointAtNothing()
			: base(new CommsRadioState(
				titleText: "Loco purchaser",
				contentText: "Aim at the locomotive you wish to purchase.",
				buttonBehaviour: ButtonBehaviourType.Regular))
		{

		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			trainCarMask = LayerMask.GetMask(new string[]
			{
			"Train_Big_Collider"
			});

			//got to steal some components from other radio modes
			refreshSignalOriginAndTrainCarMask();
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			Main.DebugLog("Calling OnAction while pointing at nothing");
			return new PointAtNothing();
		}

		private void refreshSignalOriginAndTrainCarMask()
		{
			trainCarMask = LayerMask.GetMask(new string[]
			{
			"Train_Big_Collider"
			});
			ICommsRadioMode? commsRadioMode = ControllerAPI.GetVanillaMode(VanillaMode.Clear);
			if (commsRadioMode is null)
			{
				Main.DebugLog("Could not find CommsRadioCarDeleter");
				throw new NullReferenceException();
			}
			CommsRadioCarDeleter carDeleter = (CommsRadioCarDeleter)commsRadioMode;
			signalOrigin = carDeleter.signalOrigin;
		}

		//Highlighting of locomotives happens here
		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			while (signalOrigin is null)
			{
				Main.DebugLog("signalOrigin is null for some reason");
				refreshSignalOriginAndTrainCarMask();
			}

			RaycastHit hit;
			//if we're not pointing at anything
			if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out hit, SIGNAL_RANGE, trainCarMask))
			{
				return this;
			}

			//try to get the car we're pointing at
			TrainCar selectedCar = TrainCar.Resolve(hit.transform.root);

			//if we aren't pointing at a car
			if (selectedCar is null)
			{
				return this;
			}

			//if we're pointing at a locomotive
			SimController simController = selectedCar.GetComponent<SimController>();
			if (simController is not null)
			{
				foreach (ASimInitializedController controller in simController.otherSimControllers)
				{
					//if we're a locomotive that can explode
					//This should cover all locomotives in the game but we'll see
					Main.DebugLog("Pointing at locomotive");
					return new PointAtLoco(selectedCar);
				}
			}
			else
			{
				//if this is a freight car
				CargoDamageModel cargoDamageModel = selectedCar.GetComponent<CargoDamageModel>();
				if (cargoDamageModel is not null)
				{
					Main.DebugLog("Pointing at rolling stock - nothing should happen");
					return new PointAtNothing();
				}
			}
			return this;
		}
	}
}