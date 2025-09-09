using System.Collections.Generic;

namespace MergeDungeon.Core
{
	[System.Serializable]
	public class GameState
	{
		public int version = 1;
		public int floor = 1;
		public int room = 1;
		public int meter = 0; // current advance meter value
		public List<string> inventoryItemIds = new();
		public List<string> discoveredRecipeIds = new();
	}
}


