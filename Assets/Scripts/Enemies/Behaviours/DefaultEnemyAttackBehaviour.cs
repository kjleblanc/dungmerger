using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Enemies/Turn/Default Attack", fileName = "EnemyTurn_DefaultAttack")]
    public class DefaultEnemyAttackBehaviour : DefaultEnemyMovementBehaviour
    {
        [Tooltip("Overrides the damage dealt when greater than zero. Otherwise base damage from the definition is used.")]
        [Min(0)] public int overrideDamage = 0;

        protected override int ResolveDamage(EnemyController controller)
        {
            if (overrideDamage > 0)
            {
                return overrideDamage;
            }

            return base.ResolveDamage(controller);
        }
    }
}
