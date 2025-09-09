using System;
using UnityEngine;

namespace MergeDungeon.Core
{
	[Serializable]
	public class TileReference
	{
		public TileDefinition def;
		[SerializeField] private string id;

		public TileDefinition Resolve(TileDatabase db)
		{
			if (def != null) return def;
			if (db == null) return null;
			if (!string.IsNullOrEmpty(id))
			{
				var d = db.GetById(id);
				if (d != null)
				{
					def = d;
					return d;
				}
			}
			return null;
		}

		public void Set(TileDefinition definition)
		{
			def = definition;
			id = definition != null ? definition.Id : id;
		}
	}
}


