using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Dungeon/Floor Room Pool", fileName = "FloorRoomPool")]
    public class FloorRoomPool : ScriptableObject
    {
        [Serializable]
        public class WeightedWave
        {
            public EnemyWave wave;
            [Min(0f)] public float weight = 1f;
        }

        [Header("Rooms")]
        [Min(1)] public int roomsPerFloor = 5;

        [Header("Normal Rooms")]
        public List<WeightedWave> normalWaves = new List<WeightedWave>();

        [Header("Boss Rooms")]
        public List<WeightedWave> bossWaves = new List<WeightedWave>();

        public EnemyWave RollNormalWave()
        {
            return Roll(normalWaves);
        }

        public EnemyWave RollBossWave()
        {
            return Roll(bossWaves);
        }

        private EnemyWave Roll(List<WeightedWave> list)
        {
            if (list == null || list.Count == 0) return null;
            float total = 0f;
            for (int i = 0; i < list.Count; i++) total += Mathf.Max(0f, list[i].weight);
            if (total <= 0f)
            {
                // Return first non-null
                for (int i = 0; i < list.Count; i++) if (list[i].wave != null) return list[i].wave;
                return null;
            }
            float r = UnityEngine.Random.value * total;
            float acc = 0f;
            for (int i = 0; i < list.Count; i++)
            {
                float w = Mathf.Max(0f, list[i].weight);
                if (w <= 0f) continue;
                acc += w;
                if (r <= acc) return list[i].wave;
            }
            return list[list.Count - 1].wave;
        }
    }
}

