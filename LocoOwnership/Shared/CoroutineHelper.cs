using System.Collections;
using UnityEngine;

namespace LocoOwnership.Shared
{
	public class CoroutineHelper : MonoBehaviour
	{
		private static CoroutineHelper _instance;

		public static CoroutineHelper Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new GameObject("CoroutineHelper").AddComponent<CoroutineHelper>();
					DontDestroyOnLoad(_instance.gameObject);
				}
				return _instance;
			}
		}

		public static Coroutine StartCoro(IEnumerator routine)
		{
			return Instance.StartCoroutine(routine);
		}
	}
}
