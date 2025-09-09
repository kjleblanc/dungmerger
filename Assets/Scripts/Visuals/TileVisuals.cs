using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    // Deprecated: visuals are now defined on TileDefinition assets.
    // Keeping the ScriptableObject class with no data to avoid asset load errors until assets are migrated.
    [CreateAssetMenu(menuName = "MergeDungeon/Tile Visuals (Deprecated)", fileName = "TileVisuals_Deprecated")]
    public class TileVisuals : ScriptableObject {}
}

