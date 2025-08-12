using DV;

using UnityEngine;

using CommsRadioAPI;
using DV.PointSet;

namespace LocoOwnership.Shared
{
	public class CarHighlighter
	{
		private static readonly Vector3 HIGHLIGHT_BOUNDS_EXTENSION = new Vector3(0.25f, 0.8f, 0f);
		private const float INVALID_DESTINATION_HIGHLIGHTER_DISTANCE = 20f;

		private static GameObject deleterHighlighter;
		private static CarDestinationHighlighter summoner;

		public static int trainCarMask = LayerMask.GetMask(new string[] { "Train_Big_Collider" });
		public static LayerMask trackMask = LayerMask.GetMask(new string[] { "Default" });

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region CAR HIGHLIGHTER FUNCTIONS

		public static void RefreshCarDeleterComponent()
		{
			ICommsRadioMode? commsRadioMode = ControllerAPI.GetVanillaMode(VanillaMode.Clear);
			if (commsRadioMode == null)
			{
				Debug.LogError("Could not find CommsRadioCarDeleter");
				return;
			}
			CommsRadioCarDeleter carDeleter = (CommsRadioCarDeleter)commsRadioMode;

			deleterHighlighter = carDeleter.trainHighlighter;
			deleterHighlighter.SetActive(false);
			deleterHighlighter.transform.SetParent(null);
		}

		public static void StartSelectorHighlighter(CommsRadioUtility utility, TrainCar car, bool isValid = true)
		{
			if (deleterHighlighter == null)
			{
				RefreshCarDeleterComponent();
			}

			MeshRenderer highlighterRenderer = deleterHighlighter.GetComponentInChildren<MeshRenderer>(true);
			if (isValid)
			{
				highlighterRenderer.material = utility.GetMaterial(VanillaMaterial.Valid);
			}
			else
			{
				highlighterRenderer.material = utility.GetMaterial(VanillaMaterial.Invalid);
			}

			deleterHighlighter.transform.localScale = car.Bounds.size + HIGHLIGHT_BOUNDS_EXTENSION;
			Vector3 b = car.transform.up * (deleterHighlighter.transform.localScale.y / 2f);
			Vector3 b2 = car.transform.forward * car.Bounds.center.z;
			Vector3 position = car.transform.position + b + b2;

			deleterHighlighter.transform.SetPositionAndRotation(position, car.transform.rotation);
			deleterHighlighter.SetActive(true);
			deleterHighlighter.transform.SetParent(car.transform, true);
		}

		public static void StopSelectorHighlighter()
		{
			deleterHighlighter.SetActive(false);
			deleterHighlighter.transform.SetParent(null);
		}

		#endregion

		/*-----------------------------------------------------------------------------------------------------------------------*/

		#region DESTINATION HIGHLIGHTER

		public static void RefreshSummonerComponent()
		{
			ICommsRadioMode? commsRadioMode = ControllerAPI.GetVanillaMode(VanillaMode.SummonCrewVehicle);
			if (commsRadioMode == null)
			{
				Debug.LogError("Could not find CommsRadioCarSpawner");
				return;
			}
			CommsRadioCrewVehicle crewVehicleMode = (CommsRadioCrewVehicle)commsRadioMode;

			summoner = crewVehicleMode.destHighlighter;
			summoner.TurnOff();
		}

		public static void StartSpawnerHighlighter(CommsRadioUtility utility, EquiPointSet.Point? selectedPoint, Bounds selectedCarBounds, bool isPlaceable, bool isOppositeTrackDir, bool isSpawnerConfirm)
		{
			RefreshSummonerComponent();
			Vector3 position, direction;
			Material? highlightMaterial;

			if (isPlaceable)
			{
				position = (Vector3)selectedPoint.Value.position + WorldMover.currentMove;
				direction = isOppositeTrackDir ? selectedPoint.Value.forward * -1f : selectedPoint.Value.forward;
				highlightMaterial = utility.GetMaterial(VanillaMaterial.Valid);
			}
			else
			{
				if (isSpawnerConfirm)
				{
					position = (Vector3)selectedPoint.Value.position + WorldMover.currentMove;
					direction = selectedPoint.Value.forward;
					highlightMaterial = utility.GetMaterial(VanillaMaterial.Invalid);
				}
				else
				{
					position = utility.SignalOrigin.position + utility.SignalOrigin.forward * INVALID_DESTINATION_HIGHLIGHTER_DISTANCE;
					direction = utility.SignalOrigin.right;
					highlightMaterial = utility.GetMaterial(VanillaMaterial.Invalid);
				}
			}

			if (summoner == null)
			{
				Debug.LogError("Summoner highlight is null for some reason, skipping highlighting");
				return;
			}

			if (highlightMaterial != null)
				summoner.Highlight(position, direction, selectedCarBounds, highlightMaterial);
			else
				summoner.TurnOff();
		}

		public static void StopSpawnerHighlighter()
		{
			summoner.TurnOff();
		}

		#endregion
	}
}
