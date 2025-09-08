using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Loot Table", fileName = "LootTable")]
    public class LootTable : ScriptableObject
    {
        [Tooltip("Inclusive min count when a bag spawns")]
        public int minCount = 1;
        [Tooltip("Inclusive max count when a bag spawns")]
        public int maxCount = 3;

        [Serializable]
        public class Entry
        {
            public TileKind kind = TileKind.Goo;
            [Min(0f)] public float weight = 1f;
        }

        public List<Entry> entries = new List<Entry>()
        {
            new Entry { kind = TileKind.Goo, weight = 1f }
        };

        public int RollCount()
        {
            int min = Mathf.Min(minCount, maxCount);
            int max = Mathf.Max(minCount, maxCount);
            return UnityEngine.Random.Range(min, max + 1);
        }

        public TileKind RollItem()
        {
            float total = 0f;
            for (int i = 0; i < entries.Count; i++) total += Mathf.Max(0f, entries[i].weight);
            if (total <= 0f) return TileKind.Goo;

            float r = UnityEngine.Random.value * total;
            float acc = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                float w = Mathf.Max(0f, e.weight);
                if (w <= 0f) continue;
                acc += w;
                if (r <= acc)
                {
                    return e.kind;
                }
            }
            return entries[entries.Count - 1].kind;
        }
    }
}

