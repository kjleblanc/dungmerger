using System;
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
        public GameplayServicesContext(
            GridManager grid,
            BoardController board,
            TileService tiles,
            EnemySpawner enemies,
            VfxManager vfx,
            DragLayerController dragLayer,
            TileFactory tileFactory,
            TileDatabase tileDatabase,
            EnemyDatabase enemyDatabase,
            HeroVisualLibrary heroVisualLibrary,
            EnemyVisualLibrary enemyVisualLibrary,
            AbilitySpawnTable warriorSpawnTable,
            AbilitySpawnTable mageSpawnTable,
            AdvanceMeterController advanceMeter)
        {
            Grid = grid;
            Board = board;
            Tiles = tiles;
            Enemies = enemies;
            Fx = vfx;
            DragLayer = dragLayer;
            TileFactory = tileFactory;
            TileDatabase = tileDatabase;
            EnemyDatabase = enemyDatabase;
            HeroVisualLibrary = heroVisualLibrary;
            EnemyVisualLibrary = enemyVisualLibrary;
            WarriorSpawnTable = warriorSpawnTable;
            MageSpawnTable = mageSpawnTable;
            AdvanceMeter = advanceMeter;
        }

        public GridManager Grid { get; }
        public BoardController Board { get; }
        public TileService Tiles { get; }
        public EnemySpawner Enemies { get; }
        public VfxManager Fx { get; }
        public DragLayerController DragLayer { get; }
        public TileFactory TileFactory { get; }
        public TileDatabase TileDatabase { get; }
        public EnemyDatabase EnemyDatabase { get; }
        public HeroVisualLibrary HeroVisualLibrary { get; }
        public EnemyVisualLibrary EnemyVisualLibrary { get; }
        public AbilitySpawnTable WarriorSpawnTable { get; }
        public AbilitySpawnTable MageSpawnTable { get; }
        public AdvanceMeterController AdvanceMeter { get; }
    }
}

