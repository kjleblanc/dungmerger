using UnityEngine;

namespace MergeDungeon.Core
{
    public abstract class TileModule : ScriptableObject
    {
        public virtual void Configure(TileBase tile) {}
    }
}
