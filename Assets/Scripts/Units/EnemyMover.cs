using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class EnemyMover : MonoBehaviour
    {
        public GridManager grid;

        private void Awake()
        {
            if (grid == null) grid = GridManager.Instance;
        }

        public void AdvanceEnemies()
        {
            if (grid == null) grid = GridManager.Instance;
            if (grid == null) return;

            List<EnemyController> enemies = grid.GetEnemiesSnapshot();
            // iterate from top to bottom to avoid stacking in same row
            enemies.Sort((a, b) => (b?.currentCell?.y ?? 0).CompareTo(a?.currentCell?.y ?? 0));
            foreach (var e in enemies)
            {
                if (e == null) continue;
                var cell = e.currentCell;
                if (cell == null) continue;

                // If already on a hero, attack and do not move
                if (cell.hero != null)
                {
                    e.AttackHero(cell.hero, 1);
                    continue;
                }

                // Try to move down one cell towards heroes
                int nx = cell.x;
                int ny = cell.y - 1;
                var target = grid.GetCell(nx, ny);
                if (target == null)
                {
                    // reached bottom
                    continue;
                }

                if (target.enemy != null)
                {
                    // blocked by enemy
                    continue;
                }

                // If hero is directly below, do not move into the hero's cell; attack from current cell
                if (target.hero != null)
                {
                    e.AttackHero(target.hero, 1);
                    continue;
                }

                // If tile present, destroy it before moving in
                if (target.tile != null)
                {
                    Destroy(target.tile.gameObject);
                    target.tile = null;
                }

                // Move into empty (or just cleared) cell
                cell.ClearEnemyIf(e);
                target.SetEnemy(e);
            }
        }
    }
}

