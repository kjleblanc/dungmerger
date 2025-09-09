using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class EnemySpawner : MonoBehaviour
    {
        public GridManager grid;
        public EnemyController enemyPrefab;
        public EnemyVisualLibrary enemyVisualLibrary;
        public LootBagTile lootBagPrefab;
        public LootTable slimeLootTable;
        public LootTable batLootTable;
        public TileBase tilePrefabForFallback;
        [Header("Definitions")]
        public TileDefinition slimeLootBagDef;
        public TileDefinition batLootBagDef;

        private readonly List<EnemyController> _enemies = new();
        public int ActiveEnemyCount => _enemies.Count;

        private void Awake()
        {
            if (grid == null) grid = GridManager.Instance;
        }

        public void InitializeFrom(GridManager g)
        {
            grid = g;
            if (enemyPrefab == null) enemyPrefab = g.enemyPrefab;
            if (enemyVisualLibrary == null) enemyVisualLibrary = g.enemyVisualLibrary;
            if (lootBagPrefab == null) lootBagPrefab = g.lootBagPrefab;
            if (slimeLootTable == null) slimeLootTable = g.slimeLootTable;
            if (batLootTable == null) batLootTable = g.batLootTable;
            if (tilePrefabForFallback == null) tilePrefabForFallback = g.tilePrefab;
        }

        public void TrySpawnEnemyTopRows()
        {
            var candidateCells = new List<BoardCell>();
            for (int y = grid.Height - 1; y >= Mathf.Max(grid.Height - 2, 0); y--)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var c = grid.GetCell(x, y);
                    if (c.enemy == null && c.tile == null && c.hero == null)
                        candidateCells.Add(c);
                }
            }
            if (candidateCells.Count == 0) return;
            var kind = (Random.value < 0.6f) ? EnemyKind.Slime : EnemyKind.Bat;
            int baseHp = kind == EnemyKind.Slime ? 1 : 2;
            if (grid.enemyDatabase != null)
            {
                var def = grid.enemyDatabase.Get(kind);
                if (def != null) baseHp = Mathf.Max(1, def.baseHP);
            }
            TrySpawnEnemyTopRowsOfKind(kind, baseHp, isBoss: false);
        }

        public EnemyController TrySpawnEnemyTopRowsOfKind(EnemyKind kind, int baseHp, bool isBoss = false)
        {
            var candidateCells = new List<BoardCell>();
            for (int y = grid.Height - 1; y >= Mathf.Max(grid.Height - 2, 0); y--)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var c = grid.GetCell(x, y);
                    if (c.enemy == null && c.tile == null && c.hero == null)
                        candidateCells.Add(c);
                }
            }
            if (candidateCells.Count == 0) return null;

            var cell = candidateCells[Random.Range(0, candidateCells.Count)];
            var e = Instantiate(enemyPrefab);
            if (e.GetComponent<EnemyUnitMover>() == null)
            {
                e.gameObject.AddComponent<EnemyUnitMover>();
            }
            e.kind = kind;
            e.isBoss = isBoss;
            e.InitializeStats(Mathf.Max(1, baseHp));
            cell.SetEnemy(e);
            e.RefreshVisual();
            if (enemyVisualLibrary != null)
            {
                var def = enemyVisualLibrary.Get(kind);
                if (def != null)
                {
                    var vis = e.GetComponentInChildren<EnemyVisual>();
                    if (vis != null && def.overrideController != null)
                    {
                        vis.overrideController = def.overrideController;
                        vis.ApplyOverride();
                        vis.PlayIdle();
                    }
                }
            }
            _enemies.Add(e);
            grid.RaiseEnemySpawned(e);
            return e;
        }

        public void OnEnemyDied(EnemyController enemy)
        {
            _enemies.Remove(enemy);
            grid.RaiseEnemyDied(enemy);
            var cell = enemy.currentCell;
            if (cell != null)
            {
                var bagDef = enemy.kind == EnemyKind.Slime ? slimeLootBagDef : batLootBagDef;
                LootTable table = enemy.kind == EnemyKind.Slime ? slimeLootTable : batLootTable;
                if (bagDef != null && grid != null && grid.tileFactory != null)
                {
                    var bagTile = grid.tileFactory.Create(bagDef);
                    if (bagTile != null)
                    {
                        var lb = bagTile as LootBagTile;
                        if (lb != null)
                        {
                            // Init to set roll count. If def has lootTable set, lb will use def rolls.
                            lb.Init(null);
                            lb.RefreshVisual();
                        }
                        cell.SetEnemy(null);
                        cell.SetTile(bagTile);
                    }
                }
                else
                {
                    var tableFallback = table;
                    if (tableFallback == null)
                    {
                        tableFallback = ScriptableObject.CreateInstance<LootTable>();
                        tableFallback.minCount = 1;
                        tableFallback.maxCount = 1;
                        tableFallback.entries = new List<LootTable.Entry>();
                    }
                    if (lootBagPrefab != null)
                    {
                        var bag = Instantiate(lootBagPrefab);
                        bag.Init(tableFallback);
                        bag.RefreshVisual();
                        cell.SetEnemy(null);
                        cell.SetTile(bag);
                    }
                    else if (tilePrefabForFallback != null)
                    {
                        var t = Instantiate(tilePrefabForFallback);
                        t.lootRemaining = tableFallback != null ? tableFallback.RollCount() : 1;
                        t.RefreshVisual();
                        cell.SetEnemy(null);
                        cell.SetTile(t);
                    }
                }
            }
        }

        public List<EnemyController> GetEnemiesSnapshot()
        {
            return new List<EnemyController>(_enemies);
        }
    }
}
