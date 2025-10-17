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
		private CommsRadioCrewVehicle? summoner;
		private const float SIGNAL_RANGE = 100f;

		private Bounds selectedCarBounds;
		private bool isSelectedOrientationOppositeTrackDirection;

		private bool highlighterState;

		private TrainCar selectedCar;

		public RequestConfirm(
			TrainCar selectedCar,
			float carTeleportPrice,
			RailTrack track,
			Bounds carBounds,
			EquiPointSet.Point? spawnPoint,
			bool reverseDirection,
			bool highlighterState
			)
			: base(new CommsRadioState(
				titleText: LocalizationAPI.L("lo/radio/general/request"),
				contentText: LocalizationAPI.L("lo/radio/rselected/content", selectedCar.carLivery.localizationKey, selectedCar.ID, carTeleportPrice.ToString()),
				actionText: LocalizationAPI.L("comms/confirm"),
				buttonBehaviour: ButtonBehaviourType.Override))
		{
			this.selectedCar = selectedCar;
			this.carTeleportPrice = carTeleportPrice;
			this.highlighterState = highlighterState;
			selectedTrack = track;
			selectedCarBounds = carBounds;
			selectedPoint = spawnPoint;
			isSelectedOrientationOppositeTrackDirection = reverseDirection;

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
				return new OwnershipMenus(2);
			}

			if (playerMoney >= carTeleportPrice)
			{
				// teleporting loco
				if (Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, SIGNAL_RANGE, laserPointerMask))
				{
					try
					{
						utility.StartCoroutine(TeleportLoco(selectedCar));
					}
					catch (Exception ex)
					{
						Debug.LogError("An error occurred during the teleport process: " + ex.Message);
						utility.PlaySound(VanillaSoundCommsRadio.Warning);
						return new RequestFail(2);
					}
				}

				utility.PlayVehicleSound(VanillaSoundVehicle.SpawnVehicle, selectedCar);
				if (!Main.Settings.freeCarTeleport)
				{
					utility.PlaySound(VanillaSoundCommsRadio.MoneyRemoved);
				}
				return new OwnershipMenus(2);
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
			if (Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out hit, SIGNAL_RANGE, laserPointerMask))
			{
				Transform transform = hit.collider.transform;

				bool flag = transform == summoner.destinationHighlighterGO.transform || transform.parent == summoner.destinationHighlighterGO.transform;

				if (flag && !highlighterState)
				{
					return new RequestConfirm(
						selectedCar,
						carTeleportPrice,
						selectedTrack,
						selectedCarBounds,
						selectedPoint,
						isSelectedOrientationOppositeTrackDirection,
						true
						);
				}
			}
			else if (!Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out hit, SIGNAL_RANGE, laserPointerMask) && highlighterState)
			{
				return new RequestConfirm(
					selectedCar,
					carTeleportPrice,
					selectedTrack,
					selectedCarBounds,
					selectedPoint,
					isSelectedOrientationOppositeTrackDirection,
					false
					);
			}

			return this;
		}

		public override void OnEnter(CommsRadioUtility utility, AStateBehaviour? previous)
		{
			base.OnEnter(utility, previous);
			CarHighlighter.StartSpawnerHighlighter
				(
				utility,
				selectedPoint,
				selectedCarBounds,
				highlighterState,
				isSelectedOrientationOppositeTrackDirection,
				true
				);
		}

		public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
		{
			base.OnLeave(utility, next);
			if (!(next is RequestConfirm))
				CarHighlighter.StopSpawnerHighlighter();
		}

		private IEnumerator TeleportLoco(TrainCar car)
		{
			TrainCar loco = car;

			if (loco == null)
			{
				Debug.LogError("request confirm unexpected error: loco is null");
				yield break;
			}

			TrainCar tender = CarUtils.GetTender(loco);

			yield return null;
			Debug.Log("Teleporting locomotive '" + loco.name + "'", loco);
			BaseControlsOverrider controls = loco.GetComponent<SimController>()?.controlsOverrider;
			controls.DynamicBrake?.Set(0f);
			controls.Handbrake?.Set(1f);
			controls.Throttle?.Set(0f);
			controls.Reverser?.Set(0.5f);
			if (CarTypes.IsMUSteamLocomotive(loco.carType) && Main.Settings.theFunny)
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
