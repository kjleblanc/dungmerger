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
        private readonly List<EnemyBenchController.SlotMetadata> _slotMetadataCache = new();
        private readonly List<EnemyBenchController.SlotMetadata> _filteredSlots = new();

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

            var gm = ResolveGrid();
            var bench = ResolveBench(gm);
            var board = ResolveBoard(gm);
            if (bench == null) return null;

            if (!TrySelectSlot(definition, board, bench, preferredColumn: null, out var slot))
            {
                return null;
            }

            return SpawnEnemyInSlot(slot.index, definition, Mathf.Max(1, baseHp), isBoss, gm, bench);
        }

        public EnemyController TrySpawnEnemyAtCell(EnemyDefinition definition, int baseHp, bool isBoss, BoardCell preferredCell, bool allowFallback = false)
        {
            if (definition == null)
            {
                return null;
            }

            var gm = ResolveGrid();
            var bench = ResolveBench(gm);
            var board = ResolveBoard(gm);
            if (bench == null)
            {
                return null;
            }

            int? desiredColumn = preferredCell != null ? preferredCell.x : (int?)null;

            if (TrySelectSlot(definition, board, bench, desiredColumn, out var slot))
            {
                return SpawnEnemyInSlot(slot.index, definition, Mathf.Max(1, baseHp), isBoss, gm, bench);
            }

            if (!allowFallback)
            {
                return null;
            }

            if (TrySelectSlot(definition, board, bench, preferredColumn: null, out slot))
            {
                return SpawnEnemyInSlot(slot.index, definition, Mathf.Max(1, baseHp), isBoss, gm, bench);
            }

            return null;
        }

        private GridManager ResolveGrid()
        {
            return services != null ? services.Grid : grid;
        }

        private BoardController ResolveBoard(GridManager gm)
        {
            return services != null ? services.Board : (gm != null ? gm.boardController : null);
        }

        private EnemyBenchController ResolveBench(GridManager gm)
        {
            if (services != null && services.EnemyBench != null)
            {
                return services.EnemyBench;
            }

            if (gm != null)
            {
                if (gm.enemyBench != null)
                {
                    return gm.enemyBench;
                }
                if (gm.enemySpawner != null && gm.enemySpawner != this && gm.enemySpawner.enemyPrefab != null)
                {
                    return gm.enemyBench;
                }
            }

            return null;
        }

        private EnemyController SpawnEnemyInSlot(int slotIndex, EnemyDefinition definition, int baseHp, bool isBoss, GridManager gm, EnemyBenchController bench)
        {
            if (enemyPrefab == null || bench == null)
            {
                return null;
            }

            if (!bench.TryGetSlotMetadata(slotIndex, out var metadata))
            {
                return null;
            }

            var enemy = Instantiate(enemyPrefab);
            enemy.SetupSpawn(definition, Mathf.Max(1, baseHp), isBoss);

            if (!bench.TryAssignEnemyToSlot(enemy, metadata.index))
            {
                Destroy(enemy.gameObject);
                return null;
            }

            enemy.AssignBenchSlot(bench, metadata.index);
            ApplyFallbackVisual(enemy, definition);

            _enemies.Add(enemy);
            gm?.RaiseEnemySpawned(enemy);
            return enemy;
        }

        private bool TrySelectSlot(EnemyDefinition definition, BoardController board, EnemyBenchController bench, int? preferredColumn, out EnemyBenchController.SlotMetadata selected)
        {
            selected = default;
            if (bench == null)
            {
                return false;
            }

            _slotMetadataCache.Clear();
            var snapshot = bench.GetSlotMetadataSnapshot(_slotMetadataCache);
            if (snapshot == null || snapshot.Count == 0)
            {
                return false;
            }

            var unlocked = new List<EnemyBenchController.SlotMetadata>();
            var locked = new List<EnemyBenchController.SlotMetadata>();

            var rawSlots = bench.Slots;
            for (int i = 0; i < snapshot.Count; i++)
            {
                var meta = snapshot[i];
                if (rawSlots == null || meta.index < 0 || meta.index >= rawSlots.Count)
                {
                    continue;
                }

                var slot = rawSlots[meta.index];
                if (slot == null) continue;
                if (slot.occupant != null) continue;

                if (slot.lockUntilExplicit)
                    locked.Add(meta);
                else
                    unlocked.Add(meta);
            }

            if (unlocked.Count == 0 && locked.Count == 0)
            {
                return false;
            }

            var desiredColumns = BuildDesiredColumns(definition != null ? definition.SpawnProfile : EnemySpawnProfile.Default, board, preferredColumn);

            if (desiredColumns.Count > 0)
            {
                var matches = FilterByColumn(unlocked, desiredColumns);
                if (matches.Count == 0)
                {
                    matches = FilterByColumn(locked, desiredColumns);
                }

                if (matches.Count > 0)
                {
                    selected = matches[Random.Range(0, matches.Count)];
                    return true;
                }
            }

            if (unlocked.Count > 0)
            {
                selected = unlocked[Random.Range(0, unlocked.Count)];
                return true;
            }

            if (locked.Count > 0)
            {
                selected = locked[Random.Range(0, locked.Count)];
                return true;
            }

            return false;
        }

        private List<int> BuildDesiredColumns(EnemySpawnProfile profile, BoardController board, int? overrideColumn)
        {
            var result = new List<int>();
            if (overrideColumn.HasValue)
            {
                var gm = ResolveGrid();
                var width = board != null ? board.width : (gm != null ? gm.Width : 0);
                int column = overrideColumn.Value;
                if (width > 0)
                {
                    column = Mathf.Clamp(column, 0, Mathf.Max(0, width - 1));
                }
                result.Add(column);
                return result;
            }

            int boardWidth = board != null ? board.width : 0;
            int boardHeight = board != null ? board.height : 0;

            if (boardWidth > 0 && boardHeight > 0 && profile.TryGetExactBoardCoordinates(boardWidth, boardHeight, out var coords))
            {
                result.Add(Mathf.Clamp(coords.x, 0, Mathf.Max(0, boardWidth - 1)));
                return result;
            }

            if (boardWidth > 0 && profile.HasColumnRange)
            {
                var range = profile.ClampColumnRange(boardWidth);
                for (int col = range.x; col <= range.y; col++)
                {
                    result.Add(Mathf.Clamp(col, 0, Mathf.Max(0, boardWidth - 1)));
                }
            }

            return result;
        }

        private List<EnemyBenchController.SlotMetadata> FilterByColumn(List<EnemyBenchController.SlotMetadata> source, List<int> desiredColumns)
        {
            _filteredSlots.Clear();
            if (source == null || source.Count == 0 || desiredColumns == null || desiredColumns.Count == 0)
            {
                return _filteredSlots;
            }

            foreach (var meta in source)
            {
                if (ColumnMatches(meta, desiredColumns))
                {
                    _filteredSlots.Add(meta);
                }
            }

            return _filteredSlots;
        }

        private static bool ColumnMatches(EnemyBenchController.SlotMetadata meta, List<int> desiredColumns)
        {
            if (desiredColumns == null || desiredColumns.Count == 0)
            {
                return true;
            }

            foreach (var column in desiredColumns)
            {
                if (meta.preferredBoardColumn < 0 || meta.preferredBoardColumn == column)
                {
                    return true;
                }
            }

            return false;
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
            if (enemy == null)
            {
                return;
            }

            _enemies.Remove(enemy);

            var gm = ResolveGrid();
            gm?.RaiseEnemyDied(enemy);

            var bench = ResolveBench(gm);
            EnemyBenchController.SlotMetadata slotMeta = default;
            if (bench != null && bench.TryGetSlotMetadata(enemy, out slotMeta))
            {
                bench.ReleaseEnemy(enemy);
            }

            SpawnLoot(enemy, gm, slotMeta);
        }

        private void SpawnLoot(EnemyController enemy, GridManager gm, EnemyBenchController.SlotMetadata slotMeta)
        {
            var definition = enemy != null ? enemy.Definition : null;
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

            if (tileToPlace == null)
            {
                return;
            }

            var board = ResolveBoard(gm);
            var cell = ResolveLootCell(board, slotMeta, definition);
            if (cell == null)
            {
                return;
            }

            cell.SetTile(tileToPlace);
        }

        private BoardCell ResolveLootCell(BoardController board, EnemyBenchController.SlotMetadata slotMeta, EnemyDefinition definition)
        {
            if (board == null || board.width <= 0 || board.height <= 0)
            {
                return null;
            }

            int column = slotMeta.preferredBoardColumn;
            if (column < 0 && definition != null)
            {
                var profile = definition.SpawnProfile;
                if (profile.TryGetExactBoardCoordinates(board.width, board.height, out var coords))
                {
                    column = Mathf.Clamp(coords.x, 0, board.width - 1);
                }
                else if (profile.HasColumnRange)
                {
                    var range = profile.ClampColumnRange(board.width);
                    column = Mathf.Clamp(range.x, 0, board.width - 1);
                }
            }

            if (column < 0)
            {
                column = Mathf.Clamp(board.width / 2, 0, board.width - 1);
            }

            int topRow = Mathf.Max(0, board.height - 1);
            int targetRow = Mathf.Clamp(topRow - Mathf.Max(0, slotMeta.targetRowOffsetFromTop), 0, topRow);

            var cell = board.GetCell(column, targetRow);
            if (CellAcceptsLoot(cell))
            {
                return cell;
            }

            for (int y = topRow; y >= 0; y--)
            {
                cell = board.GetCell(column, y);
                if (CellAcceptsLoot(cell))
                {
                    return cell;
                }
            }

            for (int y = topRow; y >= 0; y--)
            {
                for (int x = 0; x < board.width; x++)
                {
                    cell = board.GetCell(x, y);
                    if (CellAcceptsLoot(cell))
                    {
                        return cell;
                    }
                }
            }

            return null;
        }

        private static bool CellAcceptsLoot(BoardCell cell)
        {
            return cell != null && cell.tile == null;
        }

        public List<EnemyController> GetEnemiesSnapshot()
        {
            return new List<EnemyController>(_enemies);
        }
    }
}
