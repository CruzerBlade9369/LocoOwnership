using System;

using DV;
using DV.Logic.Job;

using UnityEngine;

using CommsRadioAPI;
using LocoOwnership.Shared;

namespace LocoOwnership.LocoPurchaser
{
	// this class enables when ponting at a loco
	internal abstract class PurchasePointAtSomething : AStateBehaviour
	{
		private const float SIGNAL_RANGE = 200f;

		internal TrainCar selectedCar;
		private Transform signalOrigin;
		private int trainCarMask;

		/*private GameObject highlighter;*/
		private CarHighlighter highlighter;

		public PurchasePointAtSomething(TrainCar selectedCar)
			: base(new CommsRadioState(
				titleText: "Purchase",
				contentText: "Aim at the locomotive you wish to purchase.",
				actionText: "Confirm",
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;
			if (this.selectedCar is null)
			{
				Main.DebugLog("selectedCar is null");
				throw new ArgumentNullException(nameof(selectedCar));
			}

			//got to steal some components from other radio modes
			ICommsRadioMode? commsRadioMode = ControllerAPI.GetVanillaMode(VanillaMode.Clear);
			if (commsRadioMode is null)
			{
				Main.DebugLog("Could not find CommsRadioCarDeleter");
				throw new NullReferenceException();
			}

			CommsRadioCarDeleter carDeleter = (CommsRadioCarDeleter)commsRadioMode;
			signalOrigin = carDeleter.signalOrigin;
			highlighter = new CarHighlighter();
			highlighter.InitHighlighter(selectedCar, carDeleter);
		}

		public void Awake()
		{

		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			RaycastHit hit;
			//if we're not pointing at anything
			if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out hit, SIGNAL_RANGE, trainCarMask))
			{
				return new PurchasePointAtNothing();
			}
			TrainCar target = TrainCar.Resolve(hit.transform.root);
			if (target is null || target != selectedCar)
			{
				//if we stopped pointing at selectedCar and are now pointing at either
				//nothing or another train car, then go back to PointingAtNothing so
				//we can figure out what we're pointing at
				return new PurchasePointAtNothing();
			}
			return this;
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			trainCarMask = highlighter.StartHighlighter(utility, previous);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			highlighter.StopHighlighter(utility, next);
		}
	}
}
