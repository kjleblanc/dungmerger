using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class EnemySpawner : ServicesConsumerBehaviour
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

        private void Awake() {}

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
            var gm = services != null ? services.Grid : grid;
            var board = services != null ? services.Board : (gm != null ? gm.boardController : null);
            if (board == null) return;
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
            if (candidateCells.Count == 0) return;
            var kind = (Random.value < 0.6f) ? EnemyKind.Slime : EnemyKind.Bat;
            int baseHp = kind == EnemyKind.Slime ? 1 : 2;
            var enemyDb = services != null ? services.EnemyDatabase : (gm != null ? gm.enemyDatabase : null);
            if (enemyDb != null)
            {
                var def = enemyDb.Get(kind);
                if (def != null) baseHp = Mathf.Max(1, def.baseHP);
            }
            TrySpawnEnemyTopRowsOfKind(kind, baseHp, isBoss: false);
        }

        public EnemyController TrySpawnEnemyTopRowsOfKind(EnemyKind kind, int baseHp, bool isBoss = false)
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
            gm?.RaiseEnemySpawned(e);
            return e;
        }

        public void OnEnemyDied(EnemyController enemy)
        {
            _enemies.Remove(enemy);
            var gm = services != null ? services.Grid : grid;
            gm?.RaiseEnemyDied(enemy);
            var cell = enemy.currentCell;
            if (cell != null)
            {
                var bagDef = enemy.kind == EnemyKind.Slime ? slimeLootBagDef : batLootBagDef;
                LootTable table = enemy.kind == EnemyKind.Slime ? slimeLootTable : batLootTable;
                var factory = services != null ? services.TileFactory : (gm != null ? gm.tileFactory : null);
                if (bagDef != null && factory != null)
                {
                    var bagTile = factory.Create(bagDef);
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
