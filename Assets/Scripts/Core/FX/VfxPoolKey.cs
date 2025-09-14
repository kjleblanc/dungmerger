using UnityEngine;

namespace MergeDungeon.Core
{
    // Attached to spawned VFX to remember which pool to return to.
    public class VfxPoolKey : MonoBehaviour
    {
        public string effectId;
        public VfxManager manager;
    }
}

