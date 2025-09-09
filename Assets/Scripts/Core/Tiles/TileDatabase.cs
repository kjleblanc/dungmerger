using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
	[CreateAssetMenu(menuName = "MergeDungeon/Tiles/Tile Database", fileName = "TileDatabase")]
	public class TileDatabase : ScriptableObject
	{
		[SerializeField] private List<TileDefinition> tiles = new List<TileDefinition>();
		[Header("Editor Auto-Collect (optional)")]
		[Tooltip("If assigned, database will auto-collect TileDefinition assets from these folders in the editor on load.")]
		[SerializeField] private List<string> editorSearchFolders = new List<string>();

		private Dictionary<string, TileDefinition> _idMap;

		private void OnEnable() { Rebuild(); }

		public void Rebuild()
		{
			#if UNITY_EDITOR
			// Auto-collect definitions from folders if configured
			if (editorSearchFolders != null && editorSearchFolders.Count > 0)
			{
				var found = new List<TileDefinition>();
				foreach (var folder in editorSearchFolders)
				{
					if (string.IsNullOrEmpty(folder)) continue;
					string[] guids = UnityEditor.AssetDatabase.FindAssets("t:" + nameof(TileDefinition), new[] { folder });
					foreach (var guid in guids)
					{
						string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
						var def = UnityEditor.AssetDatabase.LoadAssetAtPath<TileDefinition>(path);
						if (def != null) found.Add(def);
					}
				}
				// Merge found into tiles without duplicates
				var set = new HashSet<TileDefinition>(tiles);
				foreach (var d in found) if (!set.Contains(d)) tiles.Add(d);
			}
			#endif

			_idMap = new Dictionary<string, TileDefinition>();
			foreach (var def in tiles)
			{
				if (def == null) continue;
				_idMap[def.Id] = def;
			}
		}

		public TileDefinition GetById(string id)
		{
			if (_idMap == null) Rebuild();
			if (string.IsNullOrEmpty(id)) return null;
			return _idMap.TryGetValue(id, out var def) ? def : null;
		}

		public IReadOnlyList<TileDefinition> GetAllByCategory(TileCategory category)
		{
			if (_idMap == null) Rebuild();
			var list = new List<TileDefinition>();
			foreach (var kv in _idMap)
			{
				var def = kv.Value;
				if (def != null && def.category == category) list.Add(def);
			}
			return list;
		}

		public IReadOnlyList<TileDefinition> All => tiles;
	}
}


