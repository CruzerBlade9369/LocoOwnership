using System;

using DV;
using DV.Logic.Job;

using UnityEngine;

using CommsRadioAPI;

namespace LocoOwnership.Shared
{
	// this class enables when ponting at a loco
	internal class CarHighlighter
	{
		private static readonly Vector3 HIGHLIGHT_BOUNDS_EXTENSION = new Vector3(0.25f, 0.8f, 0f);

		internal TrainCar selectedCar;
		private int trainCarMask;

		private GameObject highlighter;

		public void InitHighlighter(TrainCar selectedCar, CommsRadioCarDeleter carDeleter)
		{
			this.selectedCar = selectedCar;
			highlighter = carDeleter.trainHighlighter;
			highlighter.SetActive(false);
			highlighter.transform.SetParent(null);
		}

		public int StartHighlighter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
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

			return trainCarMask;
		}

		public void StopHighlighter(CommsRadioUtility utility, AStateBehaviour? next)
		{
			highlighter.SetActive(false);
			highlighter.transform.SetParent(null);
		}
	}
}
