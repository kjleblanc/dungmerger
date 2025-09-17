using UnityEngine;

namespace MergeDungeon.Core
{
    [RequireComponent(typeof(EnemyController))]
    public class EnemyUnitMover : ServicesConsumerBehaviour
    {
        private EnemyController _enemy;

        private void Awake()
        {
            _enemy = GetComponent<EnemyController>();
        }

        public void TryStepDown()
        {
            if (_enemy != null && _enemy.TryExecuteMovementBehaviour()) return;
            PerformDefaultStepDown();
        }

        public void PerformDefaultStepDown()
        {
            var board = services != null ? services.Board : null;
            var cell = _enemy != null ? _enemy.currentCell : null;
            if (board == null || cell == null) return;
            int nx = cell.x;
            int ny = cell.y - 1;
            var target = board.GetCell(nx, ny);
            if (target == null) return;
            if (cell.hero != null)
            {
                _enemy.TryAttackHero(cell.hero);
                return;
            }
            if (target.enemy != null) return;
            if (target.hero != null)
            {
                _enemy.TryAttackHero(target.hero);
                return;
            }
            if (target.tile != null)
            {
                Destroy(target.tile.gameObject);
                target.tile = null;
            }
            cell.ClearEnemyIf(_enemy);
            target.SetEnemy(_enemy);
        }
    }
}
