using System.Collections.Generic;
using UnityEngine;
using MergeDungeon.Core;

namespace MergeDungeon.Core
{
	public class EnemyCoordinator : ServicesConsumerBehaviour
	{
		public VoidEventChannelSO advanceTick;

		private void OnEnable()
		{
			base.OnEnable();
			if (advanceTick != null) advanceTick.Raised += OnAdvance;
		}

		private void OnDisable()
		{
			if (advanceTick != null) advanceTick.Raised -= OnAdvance;
			base.OnDisable();
		}

		private void OnAdvance()
		{
			Debug.Log("EnemyCoordinator.OnAdvance");
			var grid = services != null ? services.Grid : null;
			if (grid == null) return;
			List<EnemyController> enemies = grid.GetEnemiesSnapshot();
			if (enemies == null || enemies.Count == 0) return;
			enemies.Sort((a, b) => (b?.currentCell?.y ?? 0).CompareTo(a?.currentCell?.y ?? 0));
			for (int i = 0; i < enemies.Count; i++)
			{
				var e = enemies[i];
				if (e == null) continue;
				var mover = e.GetComponent<EnemyUnitMover>();
				if (mover != null) mover.TryStepDown();
			}
		}
	}
}


