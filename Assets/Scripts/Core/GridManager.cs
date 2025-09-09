using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MergeDungeon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeDungeon.Core
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }
        public event System.Action<EnemyController> EnemySpawned;
        public event System.Action<EnemyController> EnemyDied;
        [Header("Events")]
        public VoidEventChannelSO advanceTick;

        [Header("Board Size (moved to BoardController)")]
        public int Width => _board != null ? _board.width : 0;
        public int Height => _board != null ? _board.height : 0;

        [Header("UI Refs")]
        [Header("Modules")]
        public DragLayerController dragLayerController; // optional, auto-add

        [Header("Prefabs")]
        public BoardCell cellPrefab;
        public TileBase tilePrefab; // generic tile
        public EnemyController enemyPrefab; // generic enemy
        public HeroController heroPrefab;   // generic hero

        [Header("Gameplay")]
        public float enemySpawnInterval = 3f;
        public float enemySpawnAccel = 0.95f; // multiply interval every spawn
        public int heroesBottomRow = 0;
        public bool restrictHeroesToBottomRow = true;

        [Header("Spawning Mode")]
        public bool spawnEnemiesContinuously = false;
        public int testEnemiesOnStart = 3;

        // Layout moved to BoardController

        // Delegated controllers
        public BoardController boardController; // optional external reference
        public EnemySpawner enemySpawner; // optional external reference
        public TileService tileService; // optional external reference
        private BoardController _board;
        private EnemySpawner _enemySpawner;
        private TileService _tileService;
        public int ActiveEnemyCount => _enemySpawner != null ? _enemySpawner.ActiveEnemyCount : 0;
        public bool IsBoardReady => _board != null && _board.IsBoardReady;

        [Header("Selection")]
        public TileBase SelectedTile { get; private set; }
        private Vector2 _lastBoardSize;

        [Header("Loot Tables")]
        public LootBagTile lootBagPrefab;
        public LootTable slimeLootTable;
        public LootTable batLootTable;

        [Header("Data Assets")]
        public MergeRules mergeRules;
        public TileVisuals tileVisuals;

        [Header("Hero Spawn Tables")]
        public AbilitySpawnTable warriorSpawnTable;
        public AbilitySpawnTable mageSpawnTable;

        [Header("Combat Data")]
        public AbilityConfig abilityConfig;
        public EnemyDatabase enemyDatabase;
        public FxController fxController; // optional, auto-add
        [Header("Visuals")]
        public EnemyVisualLibrary enemyVisualLibrary;
        public HeroVisualLibrary heroVisualLibrary;
        public event System.Action EnemyAdvanced;

        // Extracted modules (auto-initialized)
        private AdvanceMeterController _advanceMeter;
        private EnemyMover _enemyMover;
        private FxController _fx;
        private DragLayerController _drag;
        [Header("Drag Layer Sorting")]
        [HideInInspector] public bool forceDragAbove = true; // moved to DragLayerController
        [HideInInspector] public int dragSortingOrder = 4500; // moved to DragLayerController

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitDragLayer();
            InitFx();
            InitEnemySpawner();
            InitTileService();
            InitBoard();
            InitMeter();
        }

        private void InitDragLayer()
        {
            _drag = dragLayerController != null ? dragLayerController : GetComponent<DragLayerController>();
            if (_drag == null) _drag = gameObject.AddComponent<DragLayerController>();
            _drag.InitializeFrom(this);
        }

        private void InitFx()
        {
            _fx = fxController != null ? fxController : GetComponent<FxController>();
            if (_fx == null) _fx = gameObject.AddComponent<FxController>();
            _fx.InitializeFrom(this, _drag);
        }

        private void InitBoard()
        {
            _board = boardController != null ? boardController : GetComponent<BoardController>();
            if (_board == null) _board = gameObject.AddComponent<BoardController>();
            _board.InitializeFrom(this);
            PlaceStartingHeroes();
            if (testEnemiesOnStart > 0)
            {
                for (int i = 0; i < testEnemiesOnStart; i++)
                {
                    TrySpawnEnemyTopRows();
                }
            }
            if (spawnEnemiesContinuously)
            {
                StartCoroutine(SpawnEnemiesLoop());
            }
        }

        private void InitMeter()
        {
            _advanceMeter = GetComponent<AdvanceMeterController>();
            if (_advanceMeter == null) _advanceMeter = gameObject.AddComponent<AdvanceMeterController>();
            _advanceMeter.InitializeFrom(this);
            RefreshEnemyAdvanceUI();
            _enemyMover = GetComponent<EnemyMover>();
            if (_enemyMover == null) _enemyMover = gameObject.AddComponent<EnemyMover>();
            _enemyMover.grid = this;
        }

        private void InitEnemySpawner()
        {
            _enemySpawner = enemySpawner != null ? enemySpawner : GetComponent<EnemySpawner>();
            if (_enemySpawner == null) _enemySpawner = gameObject.AddComponent<EnemySpawner>();
            _enemySpawner.InitializeFrom(this);
        }

        private void InitTileService()
        {
            _tileService = tileService != null ? tileService : GetComponent<TileService>();
            if (_tileService == null) _tileService = gameObject.AddComponent<TileService>();
            _tileService.InitializeFrom(this);
        }

        // Legacy setup methods removed; handled by dedicated controllers

        public void ToggleSelectTile(TileBase tile)
        {
            if (tile == null)
            {
                SetSelectedTile(null);
                return;
            }
            if (SelectedTile == tile)
            {
                SetSelectedTile(null);
            }
            else
            {
                SetSelectedTile(tile);
            }
        }

        public void SetSelectedTile(TileBase tile)
        {
            if (SelectedTile != null)
            {
                SelectedTile.SetSelected(false);
            }
            SelectedTile = tile;
            if (SelectedTile != null)
            {
                SelectedTile.SetSelected(true);
            }
        }

        public void ClearSelection()
        {
            SetSelectedTile(null);
        }

        // Wrapper spawns delegating to EnemySpawner

        private void BuildBoard()
        {
            if (_board == null)
            {
                Debug.LogError("GridManager: BoardController missing");
                return;
            }
            _board.BuildBoard(cellPrefab);
        }

        public void RegisterHeroUse()
        {
            if (_advanceMeter == null || _enemyMover == null) return;
            _advanceMeter.Increment();
            _advanceMeter.RefreshUI();
            if (_advanceMeter.IsFull())
            {
                _advanceMeter.ResetMeter();
                if (advanceTick != null) advanceTick.Raise();
                else _enemyMover.AdvanceEnemies();
                _advanceMeter.RefreshUI();
                EnemyAdvanced?.Invoke();
            }
        }

        private void RefreshEnemyAdvanceUI()
        {
            if (_advanceMeter != null) _advanceMeter.RefreshUI();
        }

        private void AdvanceEnemies()
        {
            if (_enemyMover != null) _enemyMover.AdvanceEnemies();
        }

        public System.Collections.Generic.List<EnemyController> GetEnemiesSnapshot()
        {
            return _enemySpawner != null ? _enemySpawner.GetEnemiesSnapshot() : new System.Collections.Generic.List<EnemyController>();
        }

        private void LateUpdate()
        {
            if (_board != null) _board.RecomputeGridCellSize();
        }

        private void RecomputeGridCellSize(bool force = false)
        {
            if (_board != null) _board.RecomputeGridCellSize(force);
        }

        private void PlaceStartingHeroes()
        {
            // Spawn only the Warrior on the bottom row
            var warrior = Instantiate(heroPrefab);
            warrior.kind = HeroKind.Warrior;
            warrior.spawnTable = warriorSpawnTable;
            int startX = Mathf.Clamp(1, 0, Mathf.Max(0, Width - 1)); // prefer column 1 if available
            var c1 = GetCell(startX, heroesBottomRow);
            if (c1 == null) c1 = GetCell(0, heroesBottomRow);
            if (c1 != null)
            {
                c1.SetHero(warrior);
            }
            warrior.RefreshVisual();
        }

        // Event raisers for module components
        public void RaiseEnemySpawned(EnemyController e) { EnemySpawned?.Invoke(e); }
        public void RaiseEnemyDied(EnemyController e) { EnemyDied?.Invoke(e); }

        private IEnumerator SpawnEnemiesLoop()
        {
            float interval = enemySpawnInterval;
            var wait = new WaitForSeconds(interval);
            while (true)
            {
                yield return wait;
                TrySpawnEnemyTopRows();
                interval = Mathf.Max(1.0f, interval * enemySpawnAccel);
                wait = new WaitForSeconds(interval);
            }
        }

        private void TrySpawnEnemyTopRows()
        {
            if (_enemySpawner != null) _enemySpawner.TrySpawnEnemyTopRows();
        }

        public EnemyController TrySpawnEnemyTopRowsOfKind(EnemyKind kind, int baseHp, bool isBoss = false)
        {
            return _enemySpawner != null ? _enemySpawner.TrySpawnEnemyTopRowsOfKind(kind, baseHp, isBoss) : null;
        }

        public BoardCell GetCell(int x, int y)
        {
            return _board != null ? _board.GetCell(x, y) : null;
        }

        public bool TryPlaceTileInCell(TileBase tile, BoardCell cell)
        {
            if (_tileService != null) return _tileService.TryPlaceTileInCell(tile, cell);
            if (cell == null) return false;
            if (!cell.IsFreeForTile()) return false; // Only empty cells accept tiles
            if (tile.currentCell != null)
            {
                tile.currentCell.ClearTileIf(tile);
            }
            cell.SetTile(tile);
            return true;
        }


        // Implements 3/5 rule: 3-of-a-kind -> 1 upgraded; 5-of-a-kind -> 2 upgraded.
        public bool TryMergeOnDrop(TileBase source, TileBase target)
        {
            if (_tileService != null) return _tileService.TryMergeOnDrop(source, target);
            return false;
        }

        public bool TryPlaceHeroInCell(HeroController hero, BoardCell cell)
        {
            if (hero == null || cell == null) return false;
            if (!cell.IsEmpty()) return false;
            if (restrictHeroesToBottomRow && cell.y != heroesBottomRow) return false;

            if (hero.currentCell != null)
            {
                hero.currentCell.ClearHeroIf(hero);
            }
            cell.SetHero(hero);
            return true;
        }

        public bool TrySpawnTileAtRandom(TileKind kind)
        {
            if (_tileService != null) return _tileService.TrySpawnTileAtRandom(kind);
            var empty = CollectEmptyCells();
            if (empty.Count == 0) return false;
            var cell = empty[UnityEngine.Random.Range(0, empty.Count)];
            var t = Instantiate(tilePrefab);
            t.kind = kind;
            t.RefreshVisual();
            cell.SetTile(t);
            return true;
        }

        public void SpawnTileAtRandom(TileKind kind)
        {
            if (_tileService != null) _tileService.SpawnTileAtRandom(kind);
            else TrySpawnTileAtRandom(kind);
        }

        public bool TrySpawnTileNear(BoardCell origin, TileKind kind)
        {
            if (_tileService != null) return _tileService.TrySpawnTileNear(kind, origin);
            return TrySpawnTileAtRandom(kind);
        }

        public void SpawnTileNear(BoardCell origin, TileKind kind)
        {
            if (_tileService != null) _tileService.SpawnTileNear(kind, origin);
            else TrySpawnTileNear(origin, kind);
        }

        public bool TryFeedHero(TileBase tile, HeroController hero)
        {
            if (hero == null) return false;
            if (tile.kind == TileKind.GooJelly)
            {
                hero.GainStamina(2);
                return true;
            }
            if (tile.kind == TileKind.MushroomStew)
            {
                hero.GainExp(1);
                return true;
            }
            return false;
        }

        public bool TryUseAbilityOnEnemy(TileBase tile, EnemyController enemy)
        {
            if (enemy == null) return false;

            var entry = abilityConfig != null ? abilityConfig.Get(tile.kind) : null;
            int damage = entry != null ? Mathf.Max(0, entry.damage) : 1;
            var area = entry != null ? entry.area : AbilityArea.SingleTarget;

            if (area == AbilityArea.SingleTarget)
            {
                enemy.ApplyHit(damage, tile.kind);
                return true;
            }
            else // CrossPlus area
            {
                ApplyAreaDamage(enemy.currentCell, damage);
                return true;
            }
        }

        private void ApplyAreaDamage(BoardCell center, int damage)
        {
            if (center == null) return;
            // Target + 4-neighbors
            var cells = new List<BoardCell> { center };
            cells.Add(GetCell(center.x + 1, center.y));
            cells.Add(GetCell(center.x - 1, center.y));
            cells.Add(GetCell(center.x, center.y + 1));
            cells.Add(GetCell(center.x, center.y - 1));
            foreach (var c in cells)
            {
                if (c != null && c.enemy != null)
                {
                    c.enemy.ApplyHit(damage, TileKind.Fireball);
                }
            }
        }

        public void SpawnDamagePopup(RectTransform target, int amount, Color color)
        {
            if (_fx != null) _fx.SpawnDamagePopup(target, amount, color);
        }

        // Expose drag layer and damage color for existing code
        public Transform dragLayer => _drag != null ? _drag.dragLayer : null;
        public Color damageNumberColor => _fx != null ? _fx.damageNumberColor : new Color(1f, 0.3f, 0.3f, 1f);

        

        

        public List<BoardCell> CollectEmptyCells()
        {
            return _board != null ? _board.CollectEmptyCells() : new List<BoardCell>();
        }

        // Merge logic and BFS helpers moved to TileService

        

        public void OnEnemyDied(EnemyController enemy)
        {
            if (_enemySpawner != null)
            {
                _enemySpawner.OnEnemyDied(enemy);
            }
        }
    }
}
