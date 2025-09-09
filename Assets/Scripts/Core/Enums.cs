using UnityEngine;

namespace MergeDungeon.Core
{
    // TileKind fully removed; use TileDefinition instead.

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

    // Optional: station visuals handled via station prefabs/definitions.
}
