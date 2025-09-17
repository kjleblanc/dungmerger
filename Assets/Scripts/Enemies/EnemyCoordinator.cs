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
            enemies.Sort((a, b) => (b?.currentCell?.y ?? 0).CompareTo(a?.currentCell?.y ?? 0));
            for (int i = 0; i < enemies.Count; i++)
            {
                var e = enemies[i];
                if (e == null) continue;
                var mover = e.GetComponent<EnemyUnitMover>();
                mover?.TryStepDown();
            }
        }
    }
}
