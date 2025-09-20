using System;
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
        [Header("Services")]
        public GameplayServicesChannelSO servicesChannel;
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
        public TileBase tilePrefab; // generic tile
        public EnemyController enemyPrefab; // generic enemy
        public HeroController heroPrefab;   // generic hero

        [Header("Gameplay")]
        public int heroesBottomRow = 0;
        public bool restrictHeroesToBottomRow = true;

// Layout moved to BoardController

        // Delegated controllers
        public BoardController boardController; // optional external reference
        public EnemySpawner enemySpawner; // optional external reference
        public TileService tileService; // optional external reference
        private BoardController _board;
        private EnemySpawner _enemySpawner;
        private TileService _tileService;
        private GameplayServicesContext _servicesContext;
        public int ActiveEnemyCount => _enemySpawner != null ? _enemySpawner.ActiveEnemyCount : 0;
        public bool IsBoardReady => _board != null && _board.IsBoardReady;

        [Header("Selection")]
        public TileBase SelectedTile { get; private set; }
        private Vector2 _lastBoardSize;

        [Header("Data Assets")]
        public TileDatabase tileDatabase;

        [Header("Heroes")]
        public List<HeroDefinition> heroDefinitions = new List<HeroDefinition>();
        public HeroDefinition startingHeroDefinition;

        [Header("Combat Data")]
        public EnemyDefinitionDatabase enemyDefinitionDatabase;
        public VfxManager vfxManager; // optional, auto-add
        [Header("Visuals")]
        public EnemyVisualLibrary enemyVisualLibrary;
        public HeroVisualLibrary heroVisualLibrary;

        [Header("Progression")]
        public AdvanceMeterController advanceMeterController;
        public event System.Action EnemyAdvanced;
        public TileFactory tileFactory;

        // Extracted modules (auto-initialized)
        private AdvanceMeterController _advanceMeter;
        private VfxManager _vfx;
        private DragLayerController _drag;
        [Header("Drag Layer Sorting")]
        [HideInInspector] public bool forceDragAbove = true; // moved to DragLayerController
        [HideInInspector] public int dragSortingOrder = 4500; // moved to DragLayerController

        private void Awake()
        {
            CacheServices();
            PublishServicesIfNeeded();
        }

        private void OnEnable()
        {
            CacheServices();
            if (Application.isPlaying)
            {
                BuildBoard();
            }
            _board?.RecomputeGridCellSize(force: true);
            PublishServices();
            PropagateServicesChannel();
        }

        private void Start()
        {
            CacheServices();
            if (!Application.isPlaying)
            {
                PublishServices();
                return;
            }

            // Initialize drag/fx modules
            _drag = dragLayerController != null ? dragLayerController : GetComponent<DragLayerController>();
            if (_drag == null) _drag = gameObject.AddComponent<DragLayerController>();
            _drag.Setup();

            _vfx = vfxManager != null ? vfxManager : GetComponent<VfxManager>();
            if (_vfx == null) _vfx = gameObject.AddComponent<VfxManager>();
            if (_vfx.dragLayerController == null) _vfx.dragLayerController = _drag;
            _vfx.Setup();

            _board = boardController != null ? boardController : GetComponent<BoardController>();
            if (_board == null) _board = gameObject.AddComponent<BoardController>();

            if (_advanceMeter == null)
            {
                if (advanceMeterController != null)
                    _advanceMeter = advanceMeterController;
                else
                    _advanceMeter = GetComponent<AdvanceMeterController>();
            }
            if (_advanceMeter == null)
            {
                _advanceMeter = FindFirstObjectByType<AdvanceMeterController>();
            }
            if (_advanceMeter != null)
            {
                _advanceMeter.InitializeFrom(this);
                advanceMeterController = _advanceMeter;
            }
            RefreshEnemyAdvanceUI();

            _enemySpawner = enemySpawner != null ? enemySpawner : GetComponent<EnemySpawner>();
            if (_enemySpawner == null) _enemySpawner = gameObject.AddComponent<EnemySpawner>();
            if (_enemySpawner != null) _enemySpawner.InitializeFrom(this);

            _tileService = tileService != null ? tileService : GetComponent<TileService>();
            if (_tileService == null) _tileService = gameObject.AddComponent<TileService>();

            if (tileFactory == null) tileFactory = GetComponent<TileFactory>();
            if (tileFactory == null) tileFactory = gameObject.AddComponent<TileFactory>();

            PublishServices();
            PropagateServicesChannel();

            PlaceStartingHeroes();
        }


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
            // Centralize selection visuals in UISelectionManager
            SelectedTile = tile;
            var selMgr = UISelectionManager.Instance;
            if (selMgr != null)
            {
                if (tile == null) selMgr.ClearSelection();
                else selMgr.HandleClick(tile.gameObject);
            }
        }

        public void ClearSelection()
        {
            SelectedTile = null;
            UISelectionManager.Instance?.ClearSelection();
        }

        // Wrapper spawns delegating to EnemySpawner

        private void BuildBoard()
        {
            if (_board == null)
            {
                Debug.LogError("GridManager: BoardController missing");
                return;
            }
            _board.BuildBoard();
        }

        public void RegisterHeroUse()
        {
            if (_advanceMeter == null) return;
            _advanceMeter.Increment();
            _advanceMeter.RefreshUI();
            if (_advanceMeter.IsFull())
            {
                _advanceMeter.ResetMeter();
                advanceTick?.Raise();
                EnemyAdvanced?.Invoke();
                _advanceMeter.RefreshUI();
            }
        }

        private void RefreshEnemyAdvanceUI()
        {
            if (_advanceMeter != null) _advanceMeter.RefreshUI();
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
            if (heroPrefab == null) return;

            HeroDefinition definition = startingHeroDefinition;
            if (definition == null && heroDefinitions != null && heroDefinitions.Count > 0)
            {
                definition = heroDefinitions[0];
            }
            if (definition == null) return;

            var hero = Instantiate(heroPrefab);
            hero.SetDefinition(definition, resetStats: true);

            int startX = Mathf.Clamp(1, 0, Mathf.Max(0, Width - 1));
            var cell = GetCell(startX, heroesBottomRow);
            if (cell == null) cell = GetCell(0, heroesBottomRow);
            if (cell != null)
            {
                cell.SetHero(hero);
            }
            hero.RefreshVisual();
            PropagateServicesChannel();
        }

        // Event raisers for module components
        public void RaiseEnemySpawned(EnemyController e) { EnemySpawned?.Invoke(e); }
        public void RaiseEnemyDied(EnemyController e) { EnemyDied?.Invoke(e); }

        public EnemyController TrySpawnEnemyTopRowsOfDefinition(EnemyDefinition definition, int baseHp, bool isBoss = false)
        {
            return _enemySpawner != null ? _enemySpawner.TrySpawnEnemyTopRowsOfDefinition(definition, baseHp, isBoss) : null;
        }

        public EnemyController TrySpawnEnemyAtCell(EnemyDefinition definition, int baseHp, bool isBoss, BoardCell cell, bool allowFallback = false)
        {
            return _enemySpawner != null ? _enemySpawner.TrySpawnEnemyAtCell(definition, baseHp, isBoss, cell, allowFallback) : null;
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

        // New: Merge trigger when a tile is dropped onto another tile of the same kind.
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

        public bool TryFeedHero(TileBase tile, HeroController hero)
        {
            if (hero == null) return false;
            var feedModule = tile != null ? tile.def?.FeedModule : null;
            if (feedModule == null) return false;

            int value = Mathf.Max(0, feedModule.feedValue);
            if (feedModule.feedTarget == TileDefinition.FeedTarget.Stamina)
            {
                hero.GainStamina(value);
            }
            else
            {
                hero.GainExp(value);
            }
            return true;
        }

        public bool TryUseAbilityOnEnemy(TileBase tile, EnemyController enemy)
        {
            if (enemy == null) return false;

            var abilityModule = tile != null ? tile.def?.AbilityModule : null;
            if (abilityModule == null || !abilityModule.canAttack)
            {
                return false;
            }

            int damage = Mathf.Max(0, abilityModule.damage);
            var area = abilityModule.area;

            if (area == AbilityArea.SingleTarget)
            {
                enemy.ApplyHit(damage);
                if (_vfx != null && tile != null && abilityModule.abilityVfxPrefab != null)
                {
                    var rt = enemy.GetComponent<RectTransform>();
                    _vfx.SpawnAbilityFx(rt, abilityModule.abilityVfxPrefab);
                }
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
                    c.enemy.ApplyHit(damage);
                }
            }
        }

        public void SpawnDamagePopup(RectTransform target, int amount, Color color)
        {
            if (_vfx != null)
            {
                var go = _vfx.PlayUI(_vfx.damagePopupEffectId, target);
                if (go != null)
                {
                    var popup = go.GetComponent<DamagePopup>();
                    if (popup != null)
                    {
                        popup.Set(amount, color);
                    }
                }
            }
        }

        // Expose drag layer and damage color for existing code
        public Transform dragLayer => _drag != null ? _drag.dragLayer : null;
        public Color damageNumberColor => _vfx != null ? _vfx.damageNumberColor : new Color(1f, 0.3f, 0.3f, 1f);

        

        

        public List<BoardCell> CollectEmptyCells()
        {
            return _board != null ? _board.CollectEmptyCells() : new List<BoardCell>();
        }

        // Find the nearest empty cell to a given origin using Manhattan distance
        public BoardCell FindNearestEmptyCell(BoardCell origin)
        {
            if (origin == null || _board == null) return null;
            var empties = CollectEmptyCells();
            if (empties == null || empties.Count == 0) return null;

            BoardCell best = null;
            int bestDist = int.MaxValue;
            foreach (var c in empties)
            {
                int dist = Mathf.Abs(c.x - origin.x) + Mathf.Abs(c.y - origin.y);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = c;
                }
            }
            return best;
        }

        // Merge logic and BFS helpers moved to TileService

        

        public void OnEnemyDied(EnemyController enemy)
        {
            if (_enemySpawner != null)
            {
                _enemySpawner.OnEnemyDied(enemy);
            }
        }
    
        

        private void OnDisable()
        {
            if (servicesChannel != null && _servicesContext != null)
            {
                servicesChannel.Unregister(_servicesContext);
            }
        }

        private void CacheServices()
        {
            if (_board == null) _board = boardController != null ? boardController : GetComponent<BoardController>();
            if (_enemySpawner == null) _enemySpawner = enemySpawner != null ? enemySpawner : GetComponent<EnemySpawner>();
            if (_tileService == null) _tileService = tileService != null ? tileService : GetComponent<TileService>();
            if (_vfx == null) _vfx = vfxManager != null ? vfxManager : GetComponent<VfxManager>();
            if (_drag == null) _drag = dragLayerController != null ? dragLayerController : GetComponent<DragLayerController>();
            if (tileFactory == null) tileFactory = GetComponent<TileFactory>();

            if (_advanceMeter == null)
            {
                if (advanceMeterController != null)
                    _advanceMeter = advanceMeterController;
                else
                    _advanceMeter = GetComponent<AdvanceMeterController>();
            }
            if (_advanceMeter == null)
            {
#if UNITY_2023_1_OR_NEWER
                _advanceMeter = FindFirstObjectByType<AdvanceMeterController>();
#else
                _advanceMeter = FindObjectOfType<AdvanceMeterController>();
#endif
            }
            if (_advanceMeter != null)
            {
                advanceMeterController = _advanceMeter;
            }

            if (_enemySpawner != null) _enemySpawner.InitializeFrom(this);
            if (_advanceMeter != null) _advanceMeter.InitializeFrom(this);
        }

        private void PublishServicesIfNeeded()
        {
            if (servicesChannel == null) return;
            if (_servicesContext == null)
            {
                _servicesContext = BuildServicesContext();
                servicesChannel.Register(_servicesContext);
            }
        }

        private void PublishServices()
        {
            if (servicesChannel == null) return;
            _servicesContext = BuildServicesContext();
            servicesChannel.Register(_servicesContext);
        }

        private GameplayServicesContext BuildServicesContext()
        {
            IReadOnlyList<HeroDefinition> heroDefs = heroDefinitions != null ? (IReadOnlyList<HeroDefinition>)heroDefinitions : Array.Empty<HeroDefinition>();
            return new GameplayServicesContext(
                this,
                _board,
                _tileService,
                _enemySpawner,
                _vfx,
                _drag,
                tileFactory,
                tileDatabase,
                enemyDefinitionDatabase,
                heroVisualLibrary,
                enemyVisualLibrary,
                heroDefs,
                startingHeroDefinition,
                _advanceMeter
            );
        }

        private void PropagateServicesChannel()
        {
            if (servicesChannel == null) return;
#if UNITY_2023_1_OR_NEWER
            var consumers = FindObjectsByType<ServicesConsumerBehaviour>(FindObjectsSortMode.None);
#else
            var consumers = FindObjectsOfType<ServicesConsumerBehaviour>();
#endif
            foreach (var c in consumers)
            {
                if (c != null && c.servicesChannel == null)
                {
                    c.servicesChannel = servicesChannel;
                }
            }
        }

    }
}

