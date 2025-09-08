using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Merge Rules", fileName = "MergeRules")]
    public class MergeRules : ScriptableObject
    {
        [Serializable]
        public class MergeRecipe
        {
            public TileKind input = TileKind.Goo;
            [Min(2)] public int count = 3;
            public TileKind output = TileKind.GooJelly;
        }

        public List<MergeRecipe> recipes = new();

        private Dictionary<TileKind, MergeRecipe> _map;

        private void OnEnable()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            _map = new Dictionary<TileKind, MergeRecipe>();
            foreach (var r in recipes)
            {
                if (r == null) continue;
                _map[r.input] = r;
            }
        }

        public bool TryGetRecipe(TileKind input, out MergeRecipe recipe)
        {
            if (_map == null || _map.Count != recipes.Count)
                Rebuild();
            return _map.TryGetValue(input, out recipe);
        }
    }
}

