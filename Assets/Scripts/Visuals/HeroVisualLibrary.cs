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
            public HeroDefinition definition;
            public AnimatorOverrideController overrideController;
            public Sprite defaultSprite; // optional
        }

        public List<Entry> entries = new();
        private Dictionary<HeroDefinition, Entry> _map;
        private Entry _fallback;

        private void OnEnable()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _map = new Dictionary<HeroDefinition, Entry>();
            _fallback = null;
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (e == null) continue;
                if (e.definition == null)
                {
                    _fallback = e;
                    continue;
                }
                _map[e.definition] = e;
            }
        }

        public Entry Get(HeroDefinition definition)
        {
            if (_map == null || _map.Count != (entries?.Count ?? 0))
                Rebuild();
            if (definition != null && _map != null && _map.TryGetValue(definition, out var e)) return e;
            return _fallback;
        }
    }
}
