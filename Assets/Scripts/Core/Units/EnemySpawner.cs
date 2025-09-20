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

        public EnemyController TrySpawnEnemyAtCell(EnemyDefinition definition, int baseHp, bool isBoss, BoardCell preferredCell, bool allowFallback = false)
        {
            if (definition == null)
            {
                return null;
            }

            baseHp = Mathf.Max(1, baseHp);

            var gm = services != null ? services.Grid : grid;
            var board = services != null ? services.Board : (gm != null ? gm.boardController : null);
            if (board == null)
            {
                return null;
            }

            var spawned = SpawnEnemyInCell(preferredCell, definition, baseHp, isBoss, gm);
            if (spawned != null)
            {
                return spawned;
            }

            return allowFallback ? TrySpawnEnemyTopRowsInternal(definition, baseHp, isBoss) : null;
        }

        private EnemyController TrySpawnEnemyTopRowsInternal(EnemyDefinition definition, int baseHp, bool isBoss)
        {
            var gm = services != null ? services.Grid : grid;
            var board = services != null ? services.Board : (gm != null ? gm.boardController : null);
            if (board == null || definition == null)
            {
                return null;
            }

            var profile = definition.SpawnProfile;

            if (profile.TryGetExactBoardCoordinates(board.width, board.height, out var exactCoords))
            {
                var exactCell = board.GetCell(exactCoords.x, exactCoords.y);
                var exactSpawn = SpawnEnemyInCell(exactCell, definition, baseHp, isBoss, gm);
                if (exactSpawn != null)
                {
                    return exactSpawn;
                }
            }

            var candidateCells = BuildCandidateCells(board, profile);
            if (candidateCells.Count == 0)
            {
                return null;
            }

            var cell = candidateCells[Random.Range(0, candidateCells.Count)];
            return SpawnEnemyInCell(cell, definition, baseHp, isBoss, gm);
        }

        private bool IsCellAvailable(BoardCell cell)
        {
            return cell != null && cell.enemy == null && cell.tile == null && cell.hero == null;
        }

        private EnemyController SpawnEnemyInCell(BoardCell cell, EnemyDefinition definition, int baseHp, bool isBoss, GridManager gm)
        {
            if (!IsCellAvailable(cell) || enemyPrefab == null)
            {
                return null;
            }

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

        private List<BoardCell> BuildCandidateCells(BoardController board, EnemySpawnProfile profile)
        {
            var result = new List<BoardCell>();
            if (board == null || board.width <= 0 || board.height <= 0)
            {
                return result;
            }

            var rows = new List<int>();
            if (profile.TryGetRowIndex(board.height, out var preferredRow))
            {
                rows.Add(preferredRow);
            }
            else
            {
                int top = board.height - 1;
                int second = Mathf.Max(board.height - 2, 0);
                rows.Add(top);
                if (second != top)
                {
                    rows.Add(second);
                }
            }

            int minColumn = 0;
            int maxColumn = Mathf.Max(0, board.width - 1);
            if (profile.HasColumnRange)
            {
                var range = profile.ClampColumnRange(board.width);
                minColumn = Mathf.Clamp(range.x, 0, maxColumn);
                maxColumn = Mathf.Clamp(range.y, minColumn, maxColumn);
            }

            foreach (var row in rows)
            {
                if (row < 0 || row >= board.height) continue;
                for (int x = minColumn; x <= maxColumn; x++)
                {
                    if (x < 0 || x >= board.width) continue;
                    var cell = board.GetCell(x, row);
                    if (IsCellAvailable(cell))
                    {
                        result.Add(cell);
                    }
                }
            }

            if (result.Count == 0)
            {
                for (int y = board.height - 1; y >= Mathf.Max(board.height - 2, 0); y--)
                {
                    for (int x = 0; x < board.width; x++)
                    {
                        var cell = board.GetCell(x, y);
                        if (IsCellAvailable(cell))
                        {
                            result.Add(cell);
                        }
                    }
                }
            }

            return result;
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
