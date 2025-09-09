using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
	public class DamagePopupPool : MonoBehaviour
	{
		[SerializeField] private DamagePopup prefab;
		[SerializeField] private int warmup = 0;
		private readonly Queue<DamagePopup> _queue = new();

		private void Awake()
		{
			for (int i = 0; i < warmup; i++)
			{
				var inst = Instantiate(prefab, transform);
				inst.gameObject.SetActive(false);
				_queue.Enqueue(inst);
			}
		}

		public DamagePopup Get(Transform parent = null)
		{
			DamagePopup inst = _queue.Count > 0 ? _queue.Dequeue() : Instantiate(prefab, parent != null ? parent : transform);
			if (parent != null) inst.transform.SetParent(parent, false);
			inst.gameObject.SetActive(true);
			return inst;
		}

		public void Release(DamagePopup obj)
		{
			obj.gameObject.SetActive(false);
			obj.transform.SetParent(transform, false);
			_queue.Enqueue(obj);
		}
	}
}


