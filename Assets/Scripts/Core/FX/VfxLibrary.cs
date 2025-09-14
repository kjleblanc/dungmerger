using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/VFX/Vfx Library", fileName = "VfxLibrary")]
    public class VfxLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id = ""; // e.g., "damage_popup"
            public GameObject prefab;
        }

        public List<Entry> entries = new();

        private Dictionary<string, Entry> _map;

        private void OnEnable()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _map = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.id)) continue;
                _map[e.id] = e;
            }
        }

        public GameObject GetPrefab(string id)
        {
            if (_map == null || _map.Count != (entries?.Count ?? 0))
                Rebuild();
            if (_map != null && _map.TryGetValue(id, out var e)) return e?.prefab;
            return null;
        }
    }
}

