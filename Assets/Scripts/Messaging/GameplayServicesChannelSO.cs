using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    /// <summary>
    /// Scriptable object channel that broadcasts when the gameplay services context is ready.
    /// Consumers subscribe to receive Board/Tile/Enemy/FX services without relying on singletons.
    /// </summary>
    [CreateAssetMenu(menuName = "MergeDungeon/Messaging/Gameplay Services Channel", fileName = "GameplayServicesChannel")]
    public class GameplayServicesChannelSO : ScriptableObject
    {
        private GameplayServicesContext _current;

        public GameplayServicesContext Current => _current;
        public bool HasServices => _current != null;

        public event Action<GameplayServicesContext> ServicesRegistered;
        public event Action<GameplayServicesContext> ServicesUnregistered;

        public void Register(GameplayServicesContext services)
        {
            _current = services;
            ServicesRegistered?.Invoke(_current);
        }

        public void Unregister(GameplayServicesContext services)
        {
            if (_current != services) return;
            var previous = _current;
            _current = null;
            ServicesUnregistered?.Invoke(previous);
        }
    }

    /// <summary>
    /// Aggregates the runtime services that were previously exposed via GridManager's singleton.
    /// </summary>
    public class GameplayServicesContext
    {
        private static readonly EnemyBenchController.SlotMetadata[] NoEnemySlots = Array.Empty<EnemyBenchController.SlotMetadata>();

        public GameplayServicesContext(
            GridManager grid,
            BoardController board,
            TileService tiles,
            EnemySpawner enemies,
            VfxManager vfx,
            DragLayerController dragLayer,
            HeroBenchController heroBench,
            EnemyBenchController enemyBench,
            TileFactory tileFactory,
            TileDatabase tileDatabase,
            EnemyDefinitionDatabase enemyDefinitionDatabase,
            HeroVisualLibrary heroVisualLibrary,
            EnemyVisualLibrary enemyVisualLibrary,
            IReadOnlyList<HeroDefinition> heroDefinitions,
            HeroDefinition startingHeroDefinition,
            AdvanceMeterController advanceMeter)
        {
            Grid = grid;
            Board = board;
            Tiles = tiles;
            Enemies = enemies;
            Fx = vfx;
            DragLayer = dragLayer;
            HeroBench = heroBench;
            EnemyBench = enemyBench;
            TileFactory = tileFactory;
            TileDatabase = tileDatabase;
            EnemyDefinitionDatabase = enemyDefinitionDatabase;
            HeroVisualLibrary = heroVisualLibrary;
            EnemyVisualLibrary = enemyVisualLibrary;
            HeroDefinitions = heroDefinitions ?? Array.Empty<HeroDefinition>();
            StartingHeroDefinition = startingHeroDefinition;
            AdvanceMeter = advanceMeter;
        }

        private List<EnemyBenchController.SlotMetadata> _enemyBenchSlotBuffer;

        public GridManager Grid { get; }
        public BoardController Board { get; }
        public TileService Tiles { get; }
        public EnemySpawner Enemies { get; }
        public VfxManager Fx { get; }
        public DragLayerController DragLayer { get; }
        public HeroBenchController HeroBench { get; }
        public EnemyBenchController EnemyBench { get; }
        public TileFactory TileFactory { get; }
        public TileDatabase TileDatabase { get; }
        public EnemyDefinitionDatabase EnemyDefinitionDatabase { get; }
        public HeroVisualLibrary HeroVisualLibrary { get; }
        public EnemyVisualLibrary EnemyVisualLibrary { get; }
        public IReadOnlyList<HeroDefinition> HeroDefinitions { get; }
        public HeroDefinition StartingHeroDefinition { get; }
        public AdvanceMeterController AdvanceMeter { get; }

        public bool TryGetEnemySlotMetadata(EnemyController enemy, out EnemyBenchController.SlotMetadata metadata)
        {
            metadata = default;
            return EnemyBench != null && EnemyBench.TryGetSlotMetadata(enemy, out metadata);
        }

        public IReadOnlyList<EnemyBenchController.SlotMetadata> GetEnemyBenchSlotsSnapshot()
        {
            if (EnemyBench == null)
            {
                return NoEnemySlots;
            }

            if (_enemyBenchSlotBuffer == null)
            {
                int initialCapacity = EnemyBench.Slots != null ? EnemyBench.Slots.Count : 4;
                _enemyBenchSlotBuffer = new List<EnemyBenchController.SlotMetadata>(Mathf.Max(1, initialCapacity));
            }

            var snapshot = EnemyBench.GetSlotMetadataSnapshot(_enemyBenchSlotBuffer);
            return snapshot ?? (IReadOnlyList<EnemyBenchController.SlotMetadata>)NoEnemySlots;
        }
    }
}
