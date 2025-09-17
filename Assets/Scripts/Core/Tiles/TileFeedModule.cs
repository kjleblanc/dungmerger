using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Tiles/Modules/Feed", fileName = "TileFeedModule")]
    public class TileFeedModule : TileModule
    {
        public TileDefinition.FeedTarget feedTarget = TileDefinition.FeedTarget.Stamina;
        public int feedValue = 1;
    }
}

