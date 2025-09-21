using UnityEngine;

namespace MergeDungeon.Core
{
    public abstract class EnemyTurnBehaviour : ScriptableObject
    {
        /// <summary>
        /// Called when the behaviour is assigned so it can cache references.
        /// </summary>
        public virtual void Initialize(EnemyController controller) { }

        /// <summary>
        /// Executes a single enemy turn using bench-driven context data.
        /// </summary>
        public abstract void ExecuteTurn(EnemyController controller, EnemyTurnContext context);
    }

    public readonly struct EnemyTurnContext
    {
        public EnemyTurnContext(
            GameplayServicesContext services,
            GridManager grid,
            BoardController board,
            EnemyBenchController enemyBench,
            HeroBenchController heroBench,
            EnemyBenchController.SlotMetadata? slot)
        {
            Services = services;
            Grid = grid;
            Board = board;
            EnemyBench = enemyBench;
            HeroBench = heroBench;
            Slot = slot;
        }

        public GameplayServicesContext Services { get; }
        public GridManager Grid { get; }
        public BoardController Board { get; }
        public EnemyBenchController EnemyBench { get; }
        public HeroBenchController HeroBench { get; }
        public EnemyBenchController.SlotMetadata? Slot { get; }
    }
}
