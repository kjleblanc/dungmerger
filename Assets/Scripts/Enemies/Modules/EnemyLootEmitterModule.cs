using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Enemies/Modules/Loot Emitter", fileName = "EnemyLootEmitterModule")]
    public class EnemyLootEmitterModule : ScriptableObject
    {
        [Header("Loot Container")]
        public LootContainerDefinition lootContainer;

        [Header("Overrides")]
        [Tooltip("Optional direct override if you want to roll a loot table without spawning a loot container.")]
        public LootTable directLootTable;
    }
}
