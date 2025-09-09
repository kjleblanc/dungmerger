using UnityEngine;

namespace MergeDungeon.Core
{
	public class TileFactory : MonoBehaviour
	{
		[SerializeField] private TileDatabase database;
		[SerializeField] private TileBase defaultTilePrefab;

		public TileBase Create(TileDefinition def)
		{
			if (def == null) return null;
			var prefab = def.prefabOverride != null ? def.prefabOverride : defaultTilePrefab;
			var t = Instantiate(prefab);
			t.SetDefinition(def);
			return t;
		}

		public TileBase CreateById(string id)
		{
			var def = database != null ? database.GetById(id) : null;
			return Create(def);
		}
	}
}


