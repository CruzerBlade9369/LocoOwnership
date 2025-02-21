using System;
using System.Collections;

using DV;
using DV.InventorySystem;
using DV.Localization;
using DV.PointSet;
using DV.Simulation.Cars;
using DV.ThingTypes;

using UnityEngine;

using CommsRadioAPI;

using LocoOwnership.Menus;
using LocoOwnership.Shared;

namespace LocoOwnership.LocoRequester
{
	public class RequestConfirm : AStateBehaviour
	{
		private double playerMoney;
		private float carTeleportPrice;

		private static LayerMask laserPointerMask = LayerMask.GetMask("Laser_Pointer_Target");
		private RailTrack selectedTrack;
		private EquiPointSet.Point? selectedPoint;
		private CarDestinationHighlighter destinationHighlighter;
		private CommsRadioCrewVehicle? summoner;
		private const float SIGNAL_RANGE = 100f;
		private const float INVALID_DESTINATION_HIGHLIGHTER_DISTANCE = 20f;

		private Bounds selectedCarBounds;
		private Transform signalOrigin;
		private bool isSelectedOrientationOppositeTrackDirection;

		private bool highlighterState;

		private TrainCar loco;

		public RequestConfirm(
			TrainCar loco,
			float carTeleportPrice,
			Transform signalOrigin,
			RailTrack track,
			Bounds carBounds,
			EquiPointSet.Point? spawnPoint,
			CarDestinationHighlighter highlighter,
			bool reverseDirection,
			bool highlighterState
			)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/request"),
				contentText: LocalizationAPI.L("lo/radio/rselected/content", loco.carLivery.localizationKey, loco.ID, carTeleportPrice.ToString()),
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.loco = loco;
			this.carTeleportPrice = carTeleportPrice;
			this.highlighterState = highlighterState;
			selectedTrack = track;
			selectedCarBounds = carBounds;
			selectedPoint = spawnPoint;
			isSelectedOrientationOppositeTrackDirection = reverseDirection;
			destinationHighlighter = highlighter;
			this.signalOrigin = signalOrigin;

			

			playerMoney = Inventory.Instance.PlayerMoney;

			summoner = ControllerAPI.GetVanillaMode(VanillaMode.SummonCrewVehicle) as CommsRadioCrewVehicle;
		}

		public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
		{
			if (action != InputAction.Activate)
			{
				return this;
			}

			if (!highlighterState)
			{
				utility.PlaySound(VanillaSoundCommsRadio.Cancel);
				return new LocoRequest();
			}

			if (playerMoney >= carTeleportPrice)
			{
				// teleporting loco
				if (Physics.Raycast(signalOrigin.position, signalOrigin.forward, SIGNAL_RANGE, laserPointerMask))
				{
					try
					{
						utility.StartCoroutine(TeleportLoco(loco));
					}
					catch (Exception ex)
					{
						Debug.LogError("An error occurred during the teleport process: " + ex.Message);
						utility.PlaySound(VanillaSoundCommsRadio.Warning);
						return new RequestFail(2);
					}
				}

				utility.PlayVehicleSound(VanillaSoundVehicle.SpawnVehicle, loco);
				if (!Main.settings.freeCarTeleport)
				{
					utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
				}
				return new LocoRequest();
			}
			else
			{
				utility.PlaySound(VanillaSoundCommsRadio.Warning);
				return new RequestFail(0);
			}
		}

		public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
		{
			RaycastHit hit;
			if (Physics.Raycast(signalOrigin.position, signalOrigin.forward, out hit, SIGNAL_RANGE, laserPointerMask))
			{
				Transform transform = hit.collider.transform;

				bool flag = transform == summoner.destinationHighlighterGO.transform || transform.parent == summoner.destinationHighlighterGO.transform;

				if (flag && !highlighterState)
				{
					return new RequestConfirm(
						loco,
						carTeleportPrice,
						signalOrigin,
						selectedTrack,
						selectedCarBounds,
						selectedPoint,
						destinationHighlighter,
						isSelectedOrientationOppositeTrackDirection,
						true
						);
				}
			}
			else if (!Physics.Raycast(signalOrigin.position, signalOrigin.forward, out hit, SIGNAL_RANGE, laserPointerMask) && highlighterState)
			{
				return new RequestConfirm(
					loco,
					carTeleportPrice,
					signalOrigin,
					selectedTrack,
					selectedCarBounds,
					selectedPoint,
					destinationHighlighter,
					isSelectedOrientationOppositeTrackDirection,
					false
					);
			}

			return this;
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			HighlightSpawnPoint(utility);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			if (!(next is RequestConfirm))
				destinationHighlighter.TurnOff();
		}

		private void HighlightSpawnPoint(CommsRadioUtility utility)
		{
			Vector3 position, direction;
			Material? highlightMaterial;

			position = (Vector3)selectedPoint.Value.position + WorldMover.currentMove;
			direction = selectedPoint.Value.forward;

			if (highlighterState)
			{
				if (isSelectedOrientationOppositeTrackDirection) { direction *= -1f; }
				highlightMaterial = utility.GetMaterial(VanillaMaterial.Valid);
			}
			else
			{
				highlightMaterial = utility.GetMaterial(VanillaMaterial.Invalid);
			}

			if (highlightMaterial != null)
				destinationHighlighter.Highlight(position, direction, selectedCarBounds, highlightMaterial);
			else
				destinationHighlighter.TurnOff();
		}

		private IEnumerator TeleportLoco(TrainCar car)
		{
			TrainCar loco = car;

			if (loco == null)
			{
				Debug.LogError("request confirm unexpected error: loco is null");
				yield break;
			}

			TrainCar tender = CarGetters.GetTender(loco);

			yield return null;
			Debug.Log("Teleporting locomotive '" + loco.name + "'", loco);
			BaseControlsOverrider controls = loco.GetComponent<SimController>()?.controlsOverrider;
			controls.DynamicBrake?.Set(0f);
			controls.Handbrake?.Set(1f);
			controls.Throttle?.Set(0f);
			controls.Reverser?.Set(0.5f);
			if (CarTypes.IsMUSteamLocomotive(loco.carType) && Main.settings.theFunny)
			{
				yield return CarTeleporter.Kekw(loco, tender, selectedPoint, selectedTrack);
			}
			else
			{
				yield return CarTeleporter.TeleportLocomotive(loco, tender, selectedPoint, selectedTrack, isSelectedOrientationOppositeTrackDirection);
			}
			
		}
	}
}
