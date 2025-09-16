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
            public TileDefinition enemyDefinition;
            [Min(1)] public int baseHP = 1;
            public string displayNameOverride = "";
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
                if (e == null || e.enemyDefinition == null) continue;
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

