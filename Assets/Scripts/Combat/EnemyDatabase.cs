using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Enemy Database", fileName = "EnemyDatabase")]
    public class EnemyDatabase : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public EnemyKind kind = EnemyKind.Slime;
            [Min(1)] public int baseHP = 1;
            public string displayNameOverride = "";
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
            foreach (var e in entries)
            {
                if (e == null) continue;
                _map[e.kind] = e;
            }
        }

        public Entry Get(EnemyKind kind)
        {
            if (_map == null || _map.Count != entries.Count)
                Rebuild();
            if (_map.TryGetValue(kind, out var e)) return e;
            return null;
        }
    }
}

