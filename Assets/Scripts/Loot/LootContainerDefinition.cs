using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Loot/Loot Container Definition", fileName = "LootContainer_")]
    public class LootContainerDefinition : ScriptableObject
    {
        [Header("Container Tile")]
        public TileDefinition containerTile;

        [Header("Loot")]
        public LootTable lootTable;
        [Min(0)] public int minRolls = 1;
        [Min(0)] public int maxRolls = 1;

        public int RollCount()
        {
            int min = Mathf.Max(0, minRolls);
            int max = Mathf.Max(min, maxRolls);
            return Random.Range(min, max + 1);
        }
    }
}
