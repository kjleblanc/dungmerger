using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Enemies/Enemy Definition Database", fileName = "EnemyDefinitionDatabase")]
    public class EnemyDefinitionDatabase : ScriptableObject
    {
        public List<EnemyDefinition> definitions = new();

        private Dictionary<string, EnemyDefinition> _byId;
        private Dictionary<TileDefinition, EnemyDefinition> _byTile;

        private void OnEnable()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _byId = new Dictionary<string, EnemyDefinition>();
            _byTile = new Dictionary<TileDefinition, EnemyDefinition>();
            if (definitions == null) return;
            foreach (var def in definitions)
            {
                if (def == null) continue;
                if (!_byId.ContainsKey(def.Id))
                {
                    _byId.Add(def.Id, def);
                }
                if (def.enemyTile != null)
                {
                    _byTile[def.enemyTile] = def;
                }
            }
        }

        public EnemyDefinition GetById(string id)
        {
            if (_byId == null) Rebuild();
            if (string.IsNullOrEmpty(id)) return null;
            return _byId != null && _byId.TryGetValue(id, out var def) ? def : null;
        }

        public EnemyDefinition GetByTile(TileDefinition tile)
        {
            if (_byTile == null) Rebuild();
            if (tile == null) return null;
            return _byTile != null && _byTile.TryGetValue(tile, out var def) ? def : null;
        }

        public IReadOnlyList<EnemyDefinition> All => definitions != null ? (IReadOnlyList<EnemyDefinition>)definitions : System.Array.Empty<EnemyDefinition>();
    }
}


