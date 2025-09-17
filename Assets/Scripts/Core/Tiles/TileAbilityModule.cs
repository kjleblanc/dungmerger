using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Tiles/Modules/Ability", fileName = "TileAbilityModule")]
    public class TileAbilityModule : TileModule
    {
        public bool canAttack = false;
        public int damage = 1;
        public AbilityArea area = AbilityArea.SingleTarget;
        public GameObject abilityVfxPrefab;
    }
}
