using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Tiles/Modules/Merge", fileName = "TileMergeModule")]
    public class TileMergeModule : TileModule
    {
        public TileDefinition.MergeRule threeOfAKind;
        public TileDefinition.MergeRule fiveOfAKind;
    }
}
