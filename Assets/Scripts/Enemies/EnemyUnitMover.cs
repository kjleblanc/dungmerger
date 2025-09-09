using UnityEngine;
using MergeDungeon.Core;

namespace MergeDungeon.Core
{
	[RequireComponent(typeof(EnemyController))]
	public class EnemyUnitMover : MonoBehaviour
	{
		private EnemyController _enemy;
		private GridManager _grid;

		private void Awake()
		{
			_enemy = GetComponent<EnemyController>();
			_grid = GridManager.Instance;
		}

		public void TryStepDown()
		{
			if (_grid == null) _grid = GridManager.Instance;
			var cell = _enemy != null ? _enemy.currentCell : null;
			if (_grid == null || cell == null) return;
			int nx = cell.x;
			int ny = cell.y - 1;
			var target = _grid.GetCell(nx, ny);
			if (target == null) return;
			if (cell.hero != null) { _enemy.AttackHero(cell.hero, 1); return; }
			if (target.enemy != null) return;
			if (target.hero != null) { _enemy.AttackHero(target.hero, 1); return; }
			if (target.tile != null) { Destroy(target.tile.gameObject); target.tile = null; }
			cell.ClearEnemyIf(_enemy);
			target.SetEnemy(_enemy);
		}
	}
}


