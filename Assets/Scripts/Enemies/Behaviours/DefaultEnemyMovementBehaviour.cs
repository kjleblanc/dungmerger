using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Enemies/Movement/Default Step Down", fileName = "EnemyMove_Default")]
    public class DefaultEnemyMovementBehaviour : EnemyMovementBehaviour
    {
        public override void Tick(EnemyController controller)
        {
            if (controller == null) return;
            var mover = controller.GetComponent<EnemyUnitMover>();
            if (mover != null)
            {
                mover.PerformDefaultStepDown();
            }
        }
    }
}
