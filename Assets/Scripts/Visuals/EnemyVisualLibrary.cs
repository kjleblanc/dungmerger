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
            public EnemyKind kind = EnemyKind.Slime;
            public AnimatorOverrideController overrideController;
            public Sprite defaultSprite; // optional, if you want a static fallback
        }

        public List<Entry> entries = new();
        private Dictionary<EnemyKind, Entry> _map;

        private void OnEnable()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _map = new Dictionary<EnemyKind, Entry>();
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (e == null) continue;
                _map[e.kind] = e;
            }
        }

        public Entry Get(EnemyKind kind)
        {
            if (_map == null || _map.Count != (entries?.Count ?? 0))
                Rebuild();
            if (_map != null && _map.TryGetValue(kind, out var e)) return e;
            return null;
        }
    }
}

