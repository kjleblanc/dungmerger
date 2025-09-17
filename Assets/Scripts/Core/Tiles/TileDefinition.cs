using System;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    public enum TileCategory
    {
        Ability,
        Resource,
        Food,
        Loot,
        LootBag,
        Station,
        Other
    }

    public enum AbilityArea
    {
        SingleTarget,
        CrossPlus // target + 4-neighbors
    }

    [CreateAssetMenu(menuName = "MergeDungeon/Tiles/Tile Definition", fileName = "Tile_")]
    public class TileDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Classification")]
        public TileCategory category;

        [Header("Visuals")]
        public Sprite icon;
        public Color iconTint = Color.white;
        public Sprite background;
        public Color backgroundTint = Color.white;
        public TileBase prefabOverride;

        [Header("Behaviors")]
        public bool draggable = true;

        public enum FeedTarget
        {
            Stamina,
            Exp
        }

        [Header("Modules")]
        public List<TileModule> modules = new List<TileModule>();

        public T GetModule<T>() where T : TileModule
        {
            if (modules == null) return null;
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] is T typed) return typed;
            }
            return null;
        }

        public TileAbilityModule AbilityModule => GetModule<TileAbilityModule>();
        public TileFeedModule FeedModule => GetModule<TileFeedModule>();
        public TileMergeModule MergeModule => GetModule<TileMergeModule>();

        [Serializable]
        public class MergeRule
        {
            public int countToConsume = 3;
            public TileDefinition output;
            [Min(1)] public int outputCount = 1;
        }

        [Header("Merging")]
        public TileDefinition mergesWith; // if null, same-type merges only

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    }
}
