using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    public enum AbilityArea
    {
        SingleTarget,
        CrossPlus // target + 4-neighbors
    }

    // Deprecated: ability stats now live on TileDefinition. Kept for migration only.
    [CreateAssetMenu(menuName = "MergeDungeon/Ability Config (Deprecated)", fileName = "AbilityConfig_Deprecated")]
    public class AbilityConfig : ScriptableObject {}
}
