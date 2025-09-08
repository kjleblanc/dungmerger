using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Crafting/Book", fileName = "CraftingBook")]
    public class CraftingBook : ScriptableObject
    {
        public List<CraftingRecipe> recipes = new List<CraftingRecipe>();

        public IEnumerable<CraftingRecipe> GetRecipesForStation(StationType type)
        {
            if (recipes == null) yield break;
            foreach (var r in recipes)
            {
                if (r == null) continue;
                if (r.station == type) yield return r;
            }
        }
    }
}

