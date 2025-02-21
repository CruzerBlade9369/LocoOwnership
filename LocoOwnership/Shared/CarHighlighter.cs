using System;

using DV;

using UnityEngine;

using CommsRadioAPI;

namespace LocoOwnership.Shared
{
	public class CarHighlighter
	{
		private static readonly Vector3 HIGHLIGHT_BOUNDS_EXTENSION = new Vector3(0.25f, 0.8f, 0f);

		private TrainCar selectedCar;
		private GameObject highlighter = new();

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region CAR HIGHLIGHTER FUNCTIONS

		public void InitHighlighter(TrainCar selectedCar, CommsRadioCarDeleter carDeleter)
		{
			this.selectedCar = selectedCar;
			highlighter = carDeleter.trainHighlighter;
			highlighter.transform.SetParent(null);
		}

		public void StartHighlighter(CommsRadioUtility utility, bool isValid)
		{
			MeshRenderer highlighterRenderer = highlighter.GetComponentInChildren<MeshRenderer>(true);
			if (isValid)
			{
				highlighterRenderer.material = utility.GetMaterial(VanillaMaterial.Valid);
			}
			else
			{
				highlighterRenderer.material = utility.GetMaterial(VanillaMaterial.Invalid);
			}

			highlighter.transform.localScale = selectedCar.Bounds.size + HIGHLIGHT_BOUNDS_EXTENSION;
			Vector3 b = selectedCar.transform.up * (highlighter.transform.localScale.y / 2f);
			Vector3 b2 = selectedCar.transform.forward * selectedCar.Bounds.center.z;
			Vector3 position = selectedCar.transform.position + b + b2;

			highlighter.transform.SetPositionAndRotation(position, selectedCar.transform.rotation);
			highlighter.SetActive(true);
			highlighter.transform.SetParent(selectedCar.transform, true);
		}

		public void StopHighlighter()
		{
			highlighter.SetActive(false);
			highlighter.transform.SetParent(null);
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region COMPONENT STEALERS FOR LOCO SELECTOR

		public static CommsRadioCarDeleter RefreshCarDeleterComponent()
		{
			ICommsRadioMode? commsRadioMode = ControllerAPI.GetVanillaMode(VanillaMode.Clear);
			if (commsRadioMode == null)
			{
				Main.DebugLog("Could not find CommsRadioCarDeleter");
				throw new NullReferenceException();
			}
			CommsRadioCarDeleter carDeleter = (CommsRadioCarDeleter)commsRadioMode;

			return carDeleter;
		}

		public static int RefreshTrainCarMask()
		{
			int trainCarMask = LayerMask.GetMask(new string[]
			{
			"Train_Big_Collider"
			});

			return trainCarMask;
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

	}
}
