using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    public enum AbilityArea
    {
        SingleTarget,
        CrossPlus // target + 4-neighbors
    }

    [CreateAssetMenu(menuName = "MergeDungeon/Ability Config", fileName = "AbilityConfig")]
    public class AbilityConfig : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public TileKind kind = TileKind.SwordStrike;
            public int damage = 1;
            public AbilityArea area = AbilityArea.SingleTarget;
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
