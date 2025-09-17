using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Heroes/Hero Definition", fileName = "Hero_")]
    public class HeroDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Visuals")]
        public Color backgroundColor = Color.white;
        public Sprite portrait;

        [Header("Stats")]
        [Min(1)] public int startingLevel = 1;
        [Min(0)] public int startingExp = 0;
        [Min(1)] public int expToLevel = 2;
        [Min(0)] public int startingStamina = 3;
        [Min(1)] public int maxStamina = 5;
        [Min(0)] public int startingHp = 3;
        [Min(1)] public int maxHp = 3;

        [Header("Spawning")]
        public AbilitySpawnTable spawnTable;

        [Header("Tile Link")]
        public TileDefinition heroTile;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    }
}
