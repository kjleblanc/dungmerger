using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Tile Visuals", fileName = "TileVisuals")]
    public class TileVisuals : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public TileKind kind = TileKind.Goo;
            public string displayName = "";
            public Sprite sprite;
            public Color fallbackColor = Color.white;
        }

        public List<Entry> entries = new();
        private Dictionary<TileKind, Entry> _map;

        private void OnEnable()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _map = new Dictionary<TileKind, Entry>();
            foreach (var e in entries)
            {
                if (e == null) continue;
                _map[e.kind] = e;
            }
        }

        public Entry Get(TileKind kind)
        {
            if (_map == null || _map.Count != entries.Count)
                Rebuild();
            if (_map.TryGetValue(kind, out var e)) return e;
            return null;
        }
    }
}

