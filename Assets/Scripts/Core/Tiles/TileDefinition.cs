using System;
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

                [Header("Ability")]
                public bool canAttack;
                public int damage = 1;
                public AbilityArea area = AbilityArea.SingleTarget;
                public GameObject abilityVfxPrefab;


		public enum FeedTarget
		{
			Stamina,
			Exp
		}

		[Header("Feed")]
		public bool canFeedHero;
		public FeedTarget feedTarget = FeedTarget.Stamina;
		public int feedValue = 1;

		[Header("Loot Bag")]
		public LootTable lootTable;
		[Min(0)] public int minRolls = 1;
		[Min(0)] public int maxRolls = 1;

		[Serializable]
		public class MergeRule
		{
			public int countToConsume = 3;
			public TileDefinition output;
			[Min(1)] public int outputCount = 1;
		}

		[Header("Merging")]
		public TileDefinition mergesWith; // if null, same-type merges only
		public MergeRule threeOfAKind;
		public MergeRule fiveOfAKind;

		public string Id => string.IsNullOrEmpty(id) ? name : id;
		public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
	}
}


