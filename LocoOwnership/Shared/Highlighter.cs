using System;

using DV;
using DV.Logic.Job;

using UnityEngine;

using CommsRadioAPI;

namespace LocoOwnership.Shared
{
	// this class enables when ponting at a loco
	internal class Highlighter
	{
		private const float SIGNAL_RANGE = 200f;
		private static readonly Vector3 HIGHLIGHT_BOUNDS_EXTENSION = new Vector3(0.25f, 0.8f, 0f);

		internal TrainCar selectedCar;
		private Transform signalOrigin;
		private int trainCarMask;

		private GameObject highlighter;

		public void InitHighlighter(TrainCar selectedCar)
		{
			Main.DebugLog("Init highlighter");
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
			highlighter = carDeleter.trainHighlighter;
			highlighter.SetActive(false);
			highlighter.transform.SetParent(null);
		}

		public void StartHighlighter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			Main.DebugLog("Start highlighter");
			trainCarMask = LayerMask.GetMask(new string[]
			{
			"Train_Big_Collider"
			});

			MeshRenderer highlighterRenderer = highlighter.GetComponentInChildren<MeshRenderer>(true);
			highlighterRenderer.material = utility.GetMaterial(VanillaMaterial.Valid);

			highlighter.transform.localScale = selectedCar.Bounds.size + HIGHLIGHT_BOUNDS_EXTENSION;
			Vector3 b = selectedCar.transform.up * (highlighter.transform.localScale.y / 2f);
			Vector3 b2 = selectedCar.transform.forward * selectedCar.Bounds.center.z;
			Vector3 position = selectedCar.transform.position + b + b2;

			highlighter.transform.SetPositionAndRotation(position, selectedCar.transform.rotation);
			highlighter.SetActive(true);
			highlighter.transform.SetParent(selectedCar.transform, true);
		}

		public void StopHighlighter(CommsRadioUtility utility, AStateBehaviour? next)
		{
			Main.DebugLog("Stop highlighter");
			highlighter.SetActive(false);
			highlighter.transform.SetParent(null);
		}
	}
}
