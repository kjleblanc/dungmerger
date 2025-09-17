using UnityEngine;

namespace MergeDungeon.Core
{
    public abstract class EnemyMovementBehaviour : ScriptableObject
    {
        /// <summary>
        /// Called once when the behaviour is assigned so it can cache references.
        /// </summary>
        public virtual void Initialize(EnemyController controller) {}

        /// <summary>
        /// Called when the enemy attempts to advance (e.g., during enemy advance tick).
        /// Implementations should move or queue actions as needed.
        /// </summary>
        public abstract void Tick(EnemyController controller);
    }

    public abstract class EnemyAttackBehaviour : ScriptableObject
    {
        public virtual void Initialize(EnemyController controller) {}

        /// <summary>
        /// Returns true if the enemy can attack the specified hero given current state.
        /// </summary>
        public abstract bool CanAttack(EnemyController controller, HeroController hero);

        /// <summary>
        /// Executes the attack against the provided hero.
        /// </summary>
        public abstract void PerformAttack(EnemyController controller, HeroController hero);
    }
}
