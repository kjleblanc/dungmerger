using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Visuals/Hero Visual Library", fileName = "HeroVisualLibrary")]
    public class HeroVisualLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public HeroKind kind = HeroKind.Warrior;
            public AnimatorOverrideController overrideController;
            public Sprite defaultSprite; // optional
        }

        public List<Entry> entries = new();
        private Dictionary<HeroKind, Entry> _map;

        private void OnEnable()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _map = new Dictionary<HeroKind, Entry>();
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (e == null) continue;
                _map[e.kind] = e;
            }
        }

        public Entry Get(HeroKind kind)
        {
            if (_map == null || _map.Count != (entries?.Count ?? 0))
                Rebuild();
            if (_map != null && _map.TryGetValue(kind, out var e)) return e;
            return null;
        }
    }
}

