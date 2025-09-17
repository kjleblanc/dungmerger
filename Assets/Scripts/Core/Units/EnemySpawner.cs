using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class EnemySpawner : ServicesConsumerBehaviour
    {
        public GridManager grid;
        public EnemyController enemyPrefab;
        public EnemyVisualLibrary enemyVisualLibrary;

        private readonly List<EnemyController> _enemies = new();
        public int ActiveEnemyCount => _enemies.Count;

        public void InitializeFrom(GridManager g)
        {
            grid = g;
            if (enemyPrefab == null) enemyPrefab = g.enemyPrefab;
            if (enemyVisualLibrary == null) enemyVisualLibrary = g.enemyVisualLibrary;
        }

        public EnemyController TrySpawnEnemyTopRowsOfDefinition(EnemyDefinition definition, int baseHp, bool isBoss = false)
        {
            if (definition == null) return null;
            return TrySpawnEnemyTopRowsInternal(definition, Mathf.Max(1, baseHp), isBoss);
        }

        private EnemyController TrySpawnEnemyTopRowsInternal(EnemyDefinition definition, int baseHp, bool isBoss)
        {
            var gm = services != null ? services.Grid : grid;
            var board = services != null ? services.Board : (gm != null ? gm.boardController : null);
            if (board == null) return null;

            var candidateCells = new List<BoardCell>();
            for (int y = board.height - 1; y >= Mathf.Max(board.height - 2, 0); y--)
            {
                for (int x = 0; x < board.width; x++)
                {
                    var c = board.GetCell(x, y);
                    if (c.enemy == null && c.tile == null && c.hero == null)
                        candidateCells.Add(c);
                }
            }
            if (candidateCells.Count == 0) return null;

            var cell = candidateCells[Random.Range(0, candidateCells.Count)];
            var enemy = Instantiate(enemyPrefab);
            if (enemy.GetComponent<EnemyUnitMover>() == null)
            {
                enemy.gameObject.AddComponent<EnemyUnitMover>();
            }

            enemy.SetupSpawn(definition, Mathf.Max(1, baseHp), isBoss);
            cell.SetEnemy(enemy);

            ApplyFallbackVisual(enemy, definition);

            _enemies.Add(enemy);
            gm?.RaiseEnemySpawned(enemy);
            return enemy;
        }

        private void ApplyFallbackVisual(EnemyController enemy, EnemyDefinition definition)
        {
            if (enemy == null || enemyVisualLibrary == null) return;
            if (definition != null && definition.overrideController != null) return;
            var tile = definition != null ? definition.enemyTile : null;
            if (tile == null) return;
            var visualDef = enemyVisualLibrary.Get(tile);
            if (visualDef == null) return;
            var vis = enemy.GetComponentInChildren<EnemyVisual>();
            if (vis == null) return;
            if (visualDef.overrideController != null)
            {
                vis.overrideController = visualDef.overrideController;
                vis.ApplyOverride();
                vis.PlayIdle();
            }
            else if (visualDef.defaultSprite != null)
            {
                vis.SetStaticSprite(visualDef.defaultSprite);
            }
        }

        public void OnEnemyDied(EnemyController enemy)
        {
            _enemies.Remove(enemy);
            var gm = services != null ? services.Grid : grid;
            gm?.RaiseEnemyDied(enemy);
            var cell = enemy.currentCell;
            enemy.currentCell = null;
            if (cell == null) return;

            var definition = enemy.Definition;
            var lootModule = definition != null ? definition.LootModule : null;
            var container = lootModule != null ? lootModule.lootContainer : null;
            var directTable = lootModule != null ? lootModule.directLootTable : null;
            var factory = services != null ? services.TileFactory : (gm != null ? gm.tileFactory : null);
            var tileDb = services != null ? services.TileDatabase : (gm != null ? gm.tileDatabase : null);

            if (factory == null || (container == null && (directTable == null || tileDb == null)))
            {
                return;
            }

            TileBase tileToPlace = null;

            if (container != null)
            {
                if (container.containerTile != null)
                {
                    tileToPlace = factory.Create(container.containerTile);
                    if (tileToPlace is LootBagTile bag)
                    {
                        bag.Init(container);
                    }
                }
                else if (container.lootTable != null && tileDb != null)
                {
                    var dropDef = container.lootTable.RollItemDefinition(tileDb);
                    if (dropDef != null)
                    {
                        tileToPlace = factory.Create(dropDef);
                    }
                }
            }
            else if (directTable != null && tileDb != null)
            {
                var dropDef = directTable.RollItemDefinition(tileDb);
                if (dropDef != null)
                {
                    tileToPlace = factory.Create(dropDef);
                }
            }

            if (tileToPlace != null)
            {
                cell.SetEnemy(null);
                cell.SetTile(tileToPlace);
            }
        }

        public List<EnemyController> GetEnemiesSnapshot()
        {
            return new List<EnemyController>(_enemies);
        }
    }
}
