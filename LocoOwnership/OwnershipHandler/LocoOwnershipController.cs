using UnityEngine;

namespace LocoOwnership.OwnershipHandler
{
	public class LocoOwnershipController : MonoBehaviour
	{
		public TrainCar car;
		private float unitPurchaseValue;

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
