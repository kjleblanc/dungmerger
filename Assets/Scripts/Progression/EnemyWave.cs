using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Dungeon/Enemy Wave", fileName = "EnemyWave")]
    public class EnemyWave : ScriptableObject
    {
        [Serializable]
        public class Spawn
        {
            public EnemyDefinition enemyDefinition;
            [Min(1)] public int countMin = 1;
            [Min(1)] public int countMax = 1;
            [Tooltip("If > 0, overrides base HP for this enemy.")]
            public int hpOverride = 0;
            [Tooltip("Added per floor to the resolved base HP.")]
            public int hpBonusPerFloor = 0;
            [Tooltip("Marks all spawns in this entry as boss (tinted, labeled).")]
            public bool bossFlagForAll = false;
        }

        [Header("Wave Definition")]
        public List<Spawn> spawns = new List<Spawn>();

        [Header("Spawn Cadence")]
        [Tooltip("How many enemies to spawn immediately when the room starts.")]
        [Min(0)] public int initialSpawn = 1;
        [Tooltip("How many enemies to spawn each time the advance meter fills.")]
        [Min(0)] public int perAdvanceSpawn = 1;
    }
}
