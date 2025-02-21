using System.Collections;
using System.Collections.Generic;
using System.Linq;

using DV.MultipleUnit;
using DV.PointSet;

using UnityEngine;

namespace LocoOwnership.Shared
{
	public static class CarTeleporter
	{
		private static bool isTeleportingTrain;

		public static IEnumerator TeleportLocomotive(TrainCar loco, TrainCar? tender, EquiPointSet.Point? spawnPoint, RailTrack selectedTrack, bool reverseDirection)
		{
			if (isTeleportingTrain)
			{
				Debug.LogError("Cannot teleport train, because another teleport is already in progress");
				yield break;
			}
			if (selectedTrack == null)
			{
				Debug.LogError("Track not found!");
				yield break;
			}
			if (loco == null || loco.derailed)
			{
				Debug.LogError("carToTeleport is " + ((loco == null) ? "null" : "derailed") + "! Aborting fast travel");
				yield break;
			}

			isTeleportingTrain = true;

			loco.UncoupleSelf(playAudio: false);
			MultipleUnitModule.DisconnectCablesIfMultipleUnitSupported(loco);

			if (tender != null)
			{
				tender.UncoupleSelf(playAudio: false);
				MultipleUnitModule.DisconnectCablesIfMultipleUnitSupported(tender);
			}

			yield return WaitFor.FixedUpdate;

			Vector3 spawnPos = (Vector3)spawnPoint.Value.position + WorldMover.currentMove;

			Vector3 forward = spawnPoint.Value.forward;
			if (reverseDirection) { forward *= -1f; }

			if (tender != null)
			{
				Vector3 s282WorldPos = spawnPos - forward * tender.Bounds.center.z;
				Vector3 tenderWorldPos = spawnPos - forward * (loco.Bounds.center.z + 1.4f);

				loco.MoveToTrack(selectedTrack, s282WorldPos, forward);
				loco.GetComponent<TrainCarInteriorPhysics>()?.SyncPosition();

				tender.MoveToTrack(selectedTrack, tenderWorldPos, forward);
				tender.GetComponent<TrainCarInteriorPhysics>()?.SyncPosition();

				Coupler coupler = (loco.rearCoupler);
				coupler.TryCouple(playAudio: false);

				if (coupler.IsCoupled() && coupler.coupledTo.train == tender)
				{
					MultipleUnitModule.ConnectCablesOfConnectedCouplersIfMultipleUnitSupported(coupler, coupler.coupledTo);
				}
				else
				{
					Debug.LogError("Unexpected error, cars weren't properly coupled!!", loco);
				}
			}
			else
			{
				loco.MoveToTrack(selectedTrack, spawnPos, forward);
				loco.GetComponent<TrainCarInteriorPhysics>()?.SyncPosition();
			}

			isTeleportingTrain = false;
		}

		public static IEnumerator Kekw(TrainCar loco, TrainCar tender, EquiPointSet.Point? spawnPoint, RailTrack selectedTrack)
		{
			Debug.LogError("Probably shouldn't have enabled that!");

			List<TrainCar> carsToTeleport = new List<TrainCar>()
			{
				loco,
				tender
			};

			if (isTeleportingTrain)
			{
				Debug.LogError("Cannot teleport train, because another teleport is already in progress");
				yield break;
			}

			if (spawnPoint == null || selectedTrack == null)
			{
				Debug.LogError("spawnPoint or selectedTrack is null! Aborting teleportation");
				yield break;
			}

			if (carsToTeleport == null || carsToTeleport.Count == 0 || carsToTeleport.Any(car => car == null || car.derailed))
			{
				Debug.LogError("carsToTeleport is null/empty or one of the cars is derailed! Aborting teleportation");
				yield break;
			}

			isTeleportingTrain = true;

			foreach (TrainCar item in carsToTeleport)
			{
				item.UncoupleSelf(playAudio: false);
				MultipleUnitModule.DisconnectCablesIfMultipleUnitSupported(item);
			}

			yield return WaitFor.FixedUpdate;

			Vector3 currentPos = (Vector3)spawnPoint.Value.position + WorldMover.currentMove;
			Vector3 forward = spawnPoint.Value.forward;

			for (int i = 0; i < carsToTeleport.Count; i++)
			{
				Vector3 carPosition = currentPos;
				Vector3 carForward = forward;

				carsToTeleport[i].MoveToTrack(selectedTrack, carPosition, carForward);

				carsToTeleport[i].GetComponent<TrainCarInteriorPhysics>()?.SyncPosition();

				// Update currentPos to the position where the next car should be placed
				currentPos -= carForward * carsToTeleport[i].Bounds.center.z;
			}

			isTeleportingTrain = false;
		}
	}
}
