using UnityEngine;

namespace MergeDungeon.Core
{
    public enum TileKind
    {
        None = 0,
        // Abilities
        SwordStrike,
        Cleave,
        Spark,
        Fireball,
        // Food
        Goo,
        GooJelly,
        Mushroom,
        MushroomStew,
        // Materials / loot
        BatWing,
        Bone,
        // Loot bags
        LootBagSlime,
        LootBagBat
    }

    public enum EnemyKind
    {
        Slime,
        Bat
    }

    public enum HeroKind
    {
        Warrior,
        Mage
    }

    public enum StationType
    {
        None = 0,
        Campfire,
        Alchemy
    }

    // Optional: station TileKinds for visuals (prefabs for stations should use CraftingStationTile)
    // Add more stations here as needed.
    // Note: Ensure TileVisuals has entries if you wish to show custom icons.
}
