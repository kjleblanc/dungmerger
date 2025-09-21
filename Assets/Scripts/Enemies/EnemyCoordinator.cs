using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class EnemyCoordinator : ServicesConsumerBehaviour
    {
        private GridManager _subscribedGrid;

        protected override void OnServicesReady()
        {
            base.OnServicesReady();
            var grid = services != null ? services.Grid : null;
            if (grid == null) return;

            if (_subscribedGrid != null)
            {
                _subscribedGrid.EnemyAdvanced -= OnAdvance;
            }

            _subscribedGrid = grid;
            _subscribedGrid.EnemyAdvanced += OnAdvance;
        }

        protected override void OnServicesLost()
        {
            if (_subscribedGrid != null)
            {
                _subscribedGrid.EnemyAdvanced -= OnAdvance;
                _subscribedGrid = null;
            }
            base.OnServicesLost();
        }

        private void OnAdvance()
        {
            var grid = services != null ? services.Grid : null;
            if (grid == null) return;

            List<EnemyController> enemies = grid.GetEnemiesSnapshot();
            if (enemies == null || enemies.Count == 0) return;

            var bench = services != null ? services.EnemyBench : (grid.enemyBench != null ? grid.enemyBench : null);
            if (bench != null)
            {
                enemies.Sort((a, b) => ResolveSlotIndex(a).CompareTo(ResolveSlotIndex(b)));
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null) continue;
                enemy.ExecuteTurn();
            }

            int ResolveSlotIndex(EnemyController enemy)
            {
                if (enemy == null) return int.MaxValue;
                if (enemy.TryGetBenchSlot(out var meta))
                {
                    return meta.index;
                }
                return int.MaxValue;
            }
        }
    }
}
