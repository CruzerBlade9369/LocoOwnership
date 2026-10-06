using UnityEngine;

namespace LocoOwnership.OwnershipHandler
{
	public class LocoOwnershipController : MonoBehaviour
	{
		private TrainCar car;
		private string carGuid;
		private float unitPurchaseValue;

		public TrainCar Car => car;
		public string CarGUID => carGuid;

		private void Awake()
		{
			car = GetComponent<TrainCar>();
		}

		public void Initialize(float purchaseValue)
		{
			unitPurchaseValue = purchaseValue;
		}

		public void RemoveOwnership()
		{
			Destroy(this);
		}

		public float GetUnitPurchaseValue() { return unitPurchaseValue; }
	}
}
