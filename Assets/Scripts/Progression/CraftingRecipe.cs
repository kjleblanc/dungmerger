using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Crafting/Recipe", fileName = "CraftingRecipe")]
    public class CraftingRecipe : ScriptableObject
    {
        [Serializable]
        public class Ingredient
        {
            public TileReference tile;
            [Min(1)] public int count = 1;
        }

        [Header("Requirements")]
        public StationType station = StationType.Campfire;

        [Header("Ingredients")]
        public List<Ingredient> ingredients = new List<Ingredient>();

        [Header("Output")]
        public TileReference output;
        [Min(1)] public int outputCount = 1;

        [Header("Timing")]
        [Tooltip("If > 0, crafting may take time (MVP crafts instantly).")]
        public float craftTime = 0f;
    }
}

