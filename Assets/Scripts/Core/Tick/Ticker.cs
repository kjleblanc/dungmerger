using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
	public class Ticker : MonoBehaviour
	{
		[SerializeField] private List<MonoBehaviour> tickTargets = new();
		private readonly List<ITickable> _tickables = new();

		private void Awake()
		{
			for (int i = 0; i < tickTargets.Count; i++)
			{
				var mb = tickTargets[i];
				if (mb is ITickable t && !_tickables.Contains(t))
				{
					_tickables.Add(t);
				}
			}
		}

		private void Update()
		{
			float dt = Time.deltaTime;
			for (int i = 0; i < _tickables.Count; i++)
			{
				_tickables[i].Tick(dt);
			}
		}
	}
}


