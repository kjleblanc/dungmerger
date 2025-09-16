using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Visuals/Enemy Visual Library", fileName = "EnemyVisualLibrary")]
    public class EnemyVisualLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public TileDefinition enemyDefinition;
            public AnimatorOverrideController overrideController;
            public Sprite defaultSprite; // optional, if you want a static fallback
        }

        public List<Entry> entries = new();
        private Dictionary<TileDefinition, Entry> _map;

        private void OnEnable()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _map = new Dictionary<TileDefinition, Entry>();
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (e == null) continue;
                if (e.enemyDefinition == null) continue;
                _map[e.enemyDefinition] = e;
            }
        }

        public Entry Get(TileDefinition definition)
        {
            if (_map == null)
                Rebuild();
            if (definition != null && _map != null && _map.TryGetValue(definition, out var e)) return e;
            return null;
        }
    }
}

