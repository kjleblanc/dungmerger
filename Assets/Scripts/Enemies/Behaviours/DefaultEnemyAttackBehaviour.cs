using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Enemies/Attack/Default Melee", fileName = "EnemyAttack_Default")]
    public class DefaultEnemyAttackBehaviour : EnemyAttackBehaviour
    {
        [Min(1)] public int overrideDamage = 0;

        public override bool CanAttack(EnemyController controller, HeroController hero)
        {
            return controller != null && hero != null;
        }

        public override void PerformAttack(EnemyController controller, HeroController hero)
        {
            if (controller == null || hero == null) return;
            int damage = overrideDamage > 0 ? overrideDamage : Mathf.Max(1, controller.Definition != null ? controller.Definition.baseDamage : 1);
            controller.AttackHero(hero, damage);
        }
    }
}
