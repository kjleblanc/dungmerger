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
        [Header("Services")]
        public GameplayServicesChannelSO servicesChannel;
        public event System.Action<EnemyController> EnemySpawned;
        public event System.Action<EnemyController> EnemyDied;
        [Header("Board Size (moved to BoardController)")]
        public int Width => _board != null ? _board.width : 0;
        public int Height => _board != null ? _board.height : 0;
        public bool IsEnemyTurn => _enemyTurnActive;
        public bool ArePlayerActionsLocked => _enemyTurnActive || _pendingHeroSpawns > 0;

        [Header("UI Refs")]
        public HeroBenchController heroBench; // optional, auto-add
        public EnemyBenchController enemyBench; // optional, auto-add
        [Header("Modules")]
        public DragLayerController dragLayerController; // optional, auto-add
        [Header("Prefabs")]
        public TileBase tilePrefab; // generic tile
        public EnemyController enemyPrefab; // generic enemy
        public HeroController heroPrefab;   // generic hero

// Layout moved to BoardController
        // Delegated controllers
        public BoardController boardController; // optional external reference
        public EnemySpawner enemySpawner; // optional external reference
        public TileService tileService; // optional external reference

        private BoardController _board;
        private EnemySpawner _enemySpawner;
        private TileService _tileService;
        private HeroBenchController _heroBench;
        private EnemyBenchController _enemyBench;
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
        public event System.Action EnemyTurnStarted;
        public event System.Action EnemyTurnExecuted;
        public event System.Action EnemyTurnCompleted;
        public event System.Action EnemyAdvanced;
        public TileFactory tileFactory;

        // Extracted modules (auto-initialized)
        private AdvanceMeterController _advanceMeter;
        private VfxManager _vfx;
        private DragLayerController _drag;
        private bool _enemyTurnActive;
        private int _pendingHeroSpawns;
        private Coroutine _enemyAdvanceRoutine;
        private readonly HashSet<EnemyController> _abilityTargetSet = new HashSet<EnemyController>();
        private readonly List<EnemyBenchController.SlotMetadata> _abilitySlotBuffer = new List<EnemyBenchController.SlotMetadata>();
        private readonly List<BenchTarget> _abilityTargetDescriptors = new List<BenchTarget>();

        private readonly struct BenchTarget
        {
            public readonly int column;
            public readonly int rowOffsetFromTop;

            public BenchTarget(int column, int rowOffsetFromTop)
            {
                this.column = column;
                this.rowOffsetFromTop = rowOffsetFromTop;
            }
        }

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
            if (_enemyTurnActive) return;
            _advanceMeter.Increment();
            if (!_advanceMeter.IsFull()) return;
            _advanceMeter.ResetMeter();
            BeginEnemyTurn();
            EnsureEnemyAdvanceRoutine();
        }
        public void NotifyHeroSpawnStarted()
        {
            _pendingHeroSpawns++;
        }
        public void NotifyHeroSpawnFinished()
        {
            _pendingHeroSpawns = Mathf.Max(0, _pendingHeroSpawns - 1);
        }
        private void BeginEnemyTurn()
        {
            if (_enemyTurnActive) return;
            _enemyTurnActive = true;
            EnemyTurnStarted?.Invoke();
            _advanceMeter?.RefreshUI();
        }
        private void EndEnemyTurn()
        {
            if (!_enemyTurnActive) return;
            _enemyTurnActive = false;
            EnemyTurnCompleted?.Invoke();
            _advanceMeter?.RefreshUI();
        }
        private void EnsureEnemyAdvanceRoutine()
        {
            if (_enemyAdvanceRoutine == null)
            {
                _enemyAdvanceRoutine = StartCoroutine(RunEnemyAdvanceAfterPendingActions());
            }
        }
        private IEnumerator RunEnemyAdvanceAfterPendingActions()
        {
            while (_pendingHeroSpawns > 0)
            {
                yield return null;
            }
            yield return null;
            ExecuteEnemyTurnSequence();
            EnemyTurnExecuted?.Invoke();
            EnemyAdvanced?.Invoke();
            EndEnemyTurn();
            _enemyAdvanceRoutine = null;
        }

        private void ExecuteEnemyTurnSequence()
        {
            var bench = ResolveEnemyBench();
            if (bench != null)
            {
                bench.ExecuteEnemyTurn();
                return;
            }

            var spawner = ResolveEnemySpawner();
            if (spawner == null)
            {
                return;
            }

            var enemies = spawner.GetEnemiesSnapshot();
            if (enemies == null || enemies.Count == 0)
            {
                return;
            }

            enemies.Sort((a, b) => ResolveSlotIndex(a).CompareTo(ResolveSlotIndex(b)));

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null) continue;
                enemy.ExecuteTurn();
            }

            int ResolveSlotIndex(EnemyController enemy)
            {
                if (enemy == null) return int.MaxValue;
                if (enemy.TryGetBenchSlot(out var meta))
                {
                    return meta.index;
                }
                return int.MaxValue;
            }
        }

        private EnemyBenchController ResolveEnemyBench()
        {
            if (_enemyBench != null) return _enemyBench;
            if (enemyBench != null) return enemyBench;
            return _servicesContext != null ? _servicesContext.EnemyBench : null;
        }

        private EnemySpawner ResolveEnemySpawner()
        {
            if (_enemySpawner != null) return _enemySpawner;
            return _servicesContext != null ? _servicesContext.Enemies : null;
        }

        private BoardController ResolveBoard()
        {
            if (_board != null) return _board;
            return _servicesContext != null ? _servicesContext.Board : null;
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
            CacheServices();
            var definitions = BuildStartingHeroDefinitions();
            if (definitions.Count == 0) return;

            var bench = _heroBench != null ? _heroBench : heroBench;
            foreach (var definition in definitions)
            {
                if (definition == null) continue;
                var hero = Instantiate(heroPrefab);
                hero.SetDefinition(definition, resetStats: true);
                hero.RefreshVisual();

                int slotIndex = -1;
                bool placedOnBench = bench != null && bench.TryPlaceHero(hero, out slotIndex);
                if (placedOnBench)
                {
                    hero.AssignBenchSlot(bench, slotIndex);
                }
                else
                {
                    AttachHeroFallback(hero);
                    hero.AssignBenchSlot(null, -1);
                }
            }

            PropagateServicesChannel();
        }
        private void AttachHeroFallback(HeroController hero)
        {
            if (hero == null) return;

            var rt = hero.GetComponent<RectTransform>();
            if (rt == null) return;

            Transform parent = transform;
            if (_board != null && _board.boardContainer != null)
            {
                parent = _board.boardContainer;
            }

            rt.SetParent(parent, worldPositionStays: false);
            if (parent is RectTransform)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }
            else
            {
                rt.localPosition = Vector3.zero;
            }
            rt.localScale = Vector3.one;
        }
        private List<HeroDefinition> BuildStartingHeroDefinitions()
        {
            var defs = new List<HeroDefinition>();
            if (startingHeroDefinition != null) defs.Add(startingHeroDefinition);
            if (heroDefinitions != null)
            {
                foreach (var def in heroDefinitions)
                {
                    if (def != null && !defs.Contains(def))
                    {
                        defs.Add(def);
                    }
                }
            }
            return defs;
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
            if (ArePlayerActionsLocked) return false;
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
            if (ArePlayerActionsLocked) return false;
            if (_tileService != null) return _tileService.TryMergeOnDrop(source, target);
            return false;
        }
        public bool TryFeedHero(TileBase tile, HeroController hero)
        {
            if (ArePlayerActionsLocked) return false;
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
            if (ArePlayerActionsLocked) return false;
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
            else
            {
                ApplyAreaDamage(tile, enemy, damage, abilityModule);
                return true;
            }
        }
        private void ApplyAreaDamage(TileBase sourceTile, EnemyController centerEnemy, int damage, TileAbilityModule abilityModule)
        {
            if (centerEnemy == null) return;

            _abilityTargetSet.Clear();
            _abilityTargetSet.Add(centerEnemy);

            CollectAbilityTargets(centerEnemy, abilityModule, _abilityTargetSet);

            foreach (var target in _abilityTargetSet)
            {
                if (target == null) continue;
                target.ApplyHit(damage);
                if (_vfx != null && sourceTile != null && abilityModule != null && abilityModule.abilityVfxPrefab != null)
                {
                    var rt = target.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        _vfx.SpawnAbilityFx(rt, abilityModule.abilityVfxPrefab);
                    }
                }
            }
        }

        private void CollectAbilityTargets(EnemyController centerEnemy, TileAbilityModule abilityModule, HashSet<EnemyController> results)
        {
            if (centerEnemy == null || abilityModule == null || results == null)
            {
                return;
            }

            if (abilityModule.area == AbilityArea.SingleTarget)
            {
                return;
            }

            if (!centerEnemy.TryGetBenchSlot(out var centerSlot))
            {
                return;
            }

            bool added = false;
            var bench = ResolveEnemyBench();
            if (bench != null)
            {
                added = TryCollectTargetsFromBench(bench, centerSlot, abilityModule, results);
            }

            if (!added)
            {
                var spawner = ResolveEnemySpawner();
                if (spawner != null)
                {
                    CollectTargetsBySpawnerSnapshot(spawner, centerEnemy, centerSlot, results);
                }
            }
        }

        private bool TryCollectTargetsFromBench(EnemyBenchController bench, EnemyBenchController.SlotMetadata centerSlot, TileAbilityModule abilityModule, HashSet<EnemyController> results)
        {
            var snapshot = bench.GetSlotMetadataSnapshot(_abilitySlotBuffer);
            if (snapshot == null || snapshot.Count == 0)
            {
                return false;
            }

            var board = ResolveBoard();
            int boardWidth = board != null ? board.width : 0;
            int boardHeight = board != null ? board.height : 0;

            _abilityTargetDescriptors.Clear();
            AddAbilityDescriptor(centerSlot.preferredBoardColumn, centerSlot.targetRowOffsetFromTop, boardWidth, boardHeight);

            if (abilityModule.area == AbilityArea.CrossPlus)
            {
                AddAbilityDescriptor(centerSlot.preferredBoardColumn - 1, centerSlot.targetRowOffsetFromTop, boardWidth, boardHeight);
                AddAbilityDescriptor(centerSlot.preferredBoardColumn + 1, centerSlot.targetRowOffsetFromTop, boardWidth, boardHeight);
                AddAbilityDescriptor(centerSlot.preferredBoardColumn, centerSlot.targetRowOffsetFromTop + 1, boardWidth, boardHeight);
                AddAbilityDescriptor(centerSlot.preferredBoardColumn, centerSlot.targetRowOffsetFromTop - 1, boardWidth, boardHeight);
            }

            bool added = false;
            for (int i = 0; i < snapshot.Count; i++)
            {
                var meta = snapshot[i];
                if (meta.index == centerSlot.index) continue;
                if (meta.occupant == null) continue;

                if (MatchesAnyDescriptor(meta, _abilityTargetDescriptors))
                {
                    added |= results.Add(meta.occupant);
                }
            }

            if (!added && abilityModule.area != AbilityArea.SingleTarget)
            {
                added |= TryCollectAdjacentByIndex(snapshot, centerSlot, results);
            }

            return added;
        }

        private void CollectTargetsBySpawnerSnapshot(EnemySpawner spawner, EnemyController centerEnemy, EnemyBenchController.SlotMetadata centerSlot, HashSet<EnemyController> results)
        {
            var snapshot = spawner.GetEnemiesSnapshot();
            if (snapshot == null)
            {
                return;
            }

            for (int i = 0; i < snapshot.Count; i++)
            {
                var enemy = snapshot[i];
                if (enemy == null || enemy == centerEnemy) continue;
                if (!enemy.TryGetBenchSlot(out var otherSlot)) continue;

                if (Mathf.Abs(otherSlot.index - centerSlot.index) == 1)
                {
                    results.Add(enemy);
                }
            }
        }

        private bool TryCollectAdjacentByIndex(IReadOnlyList<EnemyBenchController.SlotMetadata> snapshot, EnemyBenchController.SlotMetadata centerSlot, HashSet<EnemyController> results)
        {
            bool added = false;
            for (int i = 0; i < snapshot.Count; i++)
            {
                var meta = snapshot[i];
                if (meta.index == centerSlot.index) continue;
                if (meta.occupant == null) continue;
                if (Mathf.Abs(meta.index - centerSlot.index) == 1)
                {
                    added |= results.Add(meta.occupant);
                }
            }
            return added;
        }

        private void AddAbilityDescriptor(int column, int rowOffsetFromTop, int boardWidth, int boardHeight)
        {
            if (column < 0) return;
            if (boardWidth > 0 && column >= boardWidth) return;
            if (rowOffsetFromTop < 0) return;
            if (boardHeight > 0 && rowOffsetFromTop >= boardHeight) return;

            _abilityTargetDescriptors.Add(new BenchTarget(column, rowOffsetFromTop));
        }

        private static bool MatchesAnyDescriptor(EnemyBenchController.SlotMetadata slot, List<BenchTarget> descriptors)
        {
            if (descriptors == null || descriptors.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < descriptors.Count; i++)
            {
                var descriptor = descriptors[i];
                bool columnMatches = descriptor.column < 0 || (slot.preferredBoardColumn >= 0 && slot.preferredBoardColumn == descriptor.column);
                bool rowMatches = descriptor.rowOffsetFromTop < 0 || (slot.targetRowOffsetFromTop >= 0 && slot.targetRowOffsetFromTop == descriptor.rowOffsetFromTop);
                if (columnMatches && rowMatches)
                {
                    return true;
                }
            }

            return false;
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
        public BoardCell FindSpawnCellForHero(HeroController hero)
        {
            if (_board == null || Width <= 0 || Height <= 0) return null;

            int preferredColumn = Mathf.Clamp(Width / 2, 0, Mathf.Max(0, Width - 1));
            var bench = _heroBench != null ? _heroBench : heroBench;
            if (bench != null && hero != null)
            {
                preferredColumn = Mathf.Clamp(bench.GetPreferredColumnForHero(hero), 0, Mathf.Max(0, Width - 1));
            }

            var cell = FindFirstEmptyInColumn(preferredColumn);
            if (cell != null) return cell;

            for (int offset = 1; offset < Width; offset++)
            {
                int left = preferredColumn - offset;
                if (left >= 0)
                {
                    cell = FindFirstEmptyInColumn(left);
                    if (cell != null) return cell;
                }

                int right = preferredColumn + offset;
                if (right < Width)
                {
                    cell = FindFirstEmptyInColumn(right);
                    if (cell != null) return cell;
                }
            }

            var origin = GetCell(preferredColumn, 0) ?? GetCell(preferredColumn, Mathf.Max(0, Height - 1));
            if (origin != null)
            {
                var nearest = FindNearestEmptyCell(origin);
                if (nearest != null) return nearest;
            }

            var empties = CollectEmptyCells();
            return empties.Count > 0 ? empties[0] : null;

            BoardCell FindFirstEmptyInColumn(int column)
            {
                for (int y = 0; y < Height; y++)
                {
                    var candidate = GetCell(column, y);
                    if (candidate != null && candidate.IsEmpty()) return candidate;
                }
                return null;
            }
        }
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
            if (_enemyAdvanceRoutine != null)
            {
                StopCoroutine(_enemyAdvanceRoutine);
                _enemyAdvanceRoutine = null;
            }
            _enemyTurnActive = false;
            _pendingHeroSpawns = 0;
        }
        private void CacheServices()
        {
            if (_board == null) _board = boardController != null ? boardController : GetComponent<BoardController>();
            if (_enemySpawner == null) _enemySpawner = enemySpawner != null ? enemySpawner : GetComponent<EnemySpawner>();
            if (_tileService == null) _tileService = tileService != null ? tileService : GetComponent<TileService>();
            if (_vfx == null) _vfx = vfxManager != null ? vfxManager : GetComponent<VfxManager>();
            if (_drag == null) _drag = dragLayerController != null ? dragLayerController : GetComponent<DragLayerController>();
            if (_heroBench == null)
            {
                if (heroBench != null)
                    _heroBench = heroBench;
                else
                {
#if UNITY_2023_1_OR_NEWER
                    _heroBench = FindFirstObjectByType<HeroBenchController>();
#else
                    _heroBench = FindObjectOfType<HeroBenchController>();
#endif
                }
            }
            if (_heroBench != null) heroBench = _heroBench;
            if (_enemyBench == null)
            {
                if (enemyBench != null)
                    _enemyBench = enemyBench;
                else
                {
#if UNITY_2023_1_OR_NEWER
                    _enemyBench = FindFirstObjectByType<EnemyBenchController>();
#else
                    _enemyBench = FindObjectOfType<EnemyBenchController>();
#endif
                }
            }
            if (_enemyBench != null) enemyBench = _enemyBench;
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
                _heroBench,
                _enemyBench,
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
