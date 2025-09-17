using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class EnemySpawner : ServicesConsumerBehaviour
    {
        [System.Serializable]
        public class EnemySpawnConfig
        {
            public EnemyDefinition enemyDefinition;
            [Min(0)] public int defaultSpawnWeight = 1;
            [Min(1)] public int fallbackBaseHp = 1;
            public TileDefinition lootBagDefinition;
            public LootTable lootTable;
        }

        public GridManager grid;
        public EnemyController enemyPrefab;
        public EnemyVisualLibrary enemyVisualLibrary;
        public LootBagTile lootBagPrefab;
        public TileBase tilePrefabForFallback;
        [Header("Enemy Configs")]
        public List<EnemySpawnConfig> spawnConfigs = new();

        private readonly List<EnemyController> _enemies = new();
        public int ActiveEnemyCount => _enemies.Count;

        private void Awake() {}

        public void InitializeFrom(GridManager g)
        {
            grid = g;
            if (enemyPrefab == null) enemyPrefab = g.enemyPrefab;
            if (enemyVisualLibrary == null) enemyVisualLibrary = g.enemyVisualLibrary;
            if (lootBagPrefab == null) lootBagPrefab = g.lootBagPrefab;
            if (tilePrefabForFallback == null) tilePrefabForFallback = g.tilePrefab;
        }

        private EnemySpawnConfig SelectRandomConfig()
        {
            if (spawnConfigs == null || spawnConfigs.Count == 0) return null;
            var candidates = new List<EnemySpawnConfig>();
            var weights = new List<int>();
            int totalWeight = 0;
            foreach (var config in spawnConfigs)
            {
                if (config?.enemyDefinition == null) continue;
                int weight = Mathf.Max(0, config.defaultSpawnWeight);
                if (weight <= 0) continue;
                candidates.Add(config);
                weights.Add(weight);
                totalWeight += weight;
            }
            if (candidates.Count == 0 || totalWeight <= 0) return null;
            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative) return candidates[i];
            }
            return candidates[candidates.Count - 1];
        }

        private EnemySpawnConfig FindConfig(EnemyDefinition definition)
        {
            if (definition == null || spawnConfigs == null) return null;
            foreach (var config in spawnConfigs)
            {
                if (config?.enemyDefinition == definition) return config;
            }
            return null;
        }

        public void TrySpawnEnemyTopRows()
        {
            var config = SelectRandomConfig();
            if (config?.enemyDefinition == null) return;

            int baseHp = ResolveBaseHp(config.enemyDefinition, config.fallbackBaseHp);
            TrySpawnEnemyTopRowsOfDefinition(config.enemyDefinition, baseHp, false);
        }

        private int ResolveBaseHp(EnemyDefinition definition, int fallback)
        {
            if (definition != null) return Mathf.Max(1, definition.baseHp);
            return Mathf.Max(1, fallback);
        }

        public EnemyController TrySpawnEnemyTopRowsOfDefinition(EnemyDefinition definition, int baseHp, bool isBoss = false)
        {
            if (definition == null) return null;
            return TrySpawnEnemyTopRowsInternal(definition, baseHp, isBoss);
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
            if (cell == null) return;

            var config = FindConfig(enemy.Definition);
            var bagDef = config != null ? config.lootBagDefinition : null;
            var table = config != null ? config.lootTable : null;
            var factory = services != null ? services.TileFactory : (gm != null ? gm.tileFactory : null);
            if (bagDef != null && factory != null)
            {
                var bagTile = factory.Create(bagDef);
                if (bagTile != null)
                {
                    if (bagTile is LootBagTile lb)
                    {
                        lb.Init(null);
                        lb.RefreshVisual();
                    }
                    cell.SetEnemy(null);
                    cell.SetTile(bagTile);
                }
                return;
            }

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

        public List<EnemyController> GetEnemiesSnapshot()
        {
            return new List<EnemyController>(_enemies);
        }
    }
}
