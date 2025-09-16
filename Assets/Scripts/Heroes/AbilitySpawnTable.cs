using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Ability Spawn Table", fileName = "AbilitySpawnTable")]
    public class AbilitySpawnTable : ScriptableObject
    {
        [Header("Optional Defaults")]
        public TileDatabase fallbackDatabase;
        [Serializable]
        public class WeightedKind
        {
            public TileReference tile;
            [Min(0f)] public float weight = 1f;
        }

        [Serializable]
        public class Tier
        {
            [Min(1)] public int minLevel = 1;
            public List<WeightedKind> entries = new() { new WeightedKind() };
        }

        public List<Tier> tiers = new() { new Tier() };

        public TileDefinition RollForLevel(TileDatabase database, int level)
        {
            Tier best = null;
            foreach (var t in tiers)
            {
                if (t == null) continue;
                if (level >= t.minLevel && (best == null || t.minLevel > best.minLevel))
                    best = t;
            }
            if (best == null || best.entries == null || best.entries.Count == 0) return null;

            float total = 0f;
            foreach (var e in best.entries) total += Mathf.Max(0f, e.weight);
            if (total <= 0f) return best.entries[0].tile != null ? best.entries[0].tile.Resolve(database) : null;
            float r = UnityEngine.Random.value * total;
            float acc = 0f;
            foreach (var e in best.entries)
            {
                float w = Mathf.Max(0f, e.weight);
                if (w <= 0f) continue;
                acc += w;
                if (r <= acc) return e.tile != null ? e.tile.Resolve(database) : null;
            }
            return best.entries[best.entries.Count - 1].tile != null ? best.entries[best.entries.Count - 1].tile.Resolve(database) : null;
        }

        public TileDefinition RollForLevel(int level)
        {
            // Convenience overload: uses an optional serialized database reference
            return RollForLevel(fallbackDatabase, level);
        }
    }
}
