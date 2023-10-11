using System;
using DV;
using UnityEngine;

namespace LocoOwnership
{
	internal class CommsRadioLocoPurchaser : MonoBehaviour, ICommsRadioMode
	{
		public static CommsRadioController Controller;
		public ButtonBehaviourType ButtonBehaviour
		{
			get;
			private set;
		}

		public CommsRadioDisplay display;
		public Transform signalOrigin;
		public Material selectionMaterial;
		public Material skinningMaterial;
		public GameObject trainHighlighter;

		// Sounds
		public AudioClip HoverCarSound;
		public AudioClip SelectedCarSound;
		public AudioClip ConfirmSound;
		public AudioClip CancelSound;

		private State CurrentState;
		private LayerMask TrainCarMask;
		private RaycastHit Hit;
		private TrainCar SelectedCar = null;
		private TrainCar PointedCar = null;
		private MeshRenderer HighlighterRender;

		private const float SIGNAL_RANGE = 100f;
		private static readonly Vector3 HIGHLIGHT_BOUNDS_EXTENSION = new Vector3(0.25f, 0.8f, 0f);
		private static readonly Color LASER_COLOR = new Color(0f, 0.4f, 0.07f);

		public Color GetLaserBeamColor()
		{
			return LASER_COLOR;
		}
		public void OverrideSignalOrigin(Transform signalOrigin) => this.signalOrigin = signalOrigin;

		public bool ButtonACustomAction()
		{
			throw new NotImplementedException();
		}

		public bool ButtonBCustomAction()
		{
			throw new NotImplementedException();
		}

		public void Disable()
		{
			throw new NotImplementedException();
		}

		public void Enable()
		{
			throw new NotImplementedException();
		}

		public void OnUpdate()
		{
			throw new NotImplementedException();
		}

		public void OnUse()
		{
			throw new NotImplementedException();
		}

		public void OverrideSignalOrigin(Transform signalOrigin)
		{
			throw new NotImplementedException();
		}

		public void SetStartingDisplay()
		{
			throw new NotImplementedException();
		}
	}
}
