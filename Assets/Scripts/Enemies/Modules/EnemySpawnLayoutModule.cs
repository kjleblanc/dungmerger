using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Enemies/Modules/Spawn Layout", fileName = "EnemySpawnLayoutModule_")]
    public class EnemySpawnLayoutModule : ScriptableObject
    {
        [Tooltip("Relative cells from the layout anchor (x right, y down from the board's top-left).")]
        public List<Vector2Int> relativeCells = new List<Vector2Int> { Vector2Int.zero };

        public IReadOnlyList<Vector2Int> Cells => relativeCells;

        private void OnValidate()
        {
            if (relativeCells == null)
            {
                relativeCells = new List<Vector2Int>();
            }
        }
    }
}
