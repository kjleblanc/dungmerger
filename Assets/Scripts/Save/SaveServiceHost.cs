using System.Threading.Tasks;
using UnityEngine;

namespace MergeDungeon.Core
{
	public class SaveServiceHost : MonoBehaviour
	{
		public static SaveServiceHost Instance { get; private set; }
		public ISaveService Service { get; private set; }

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}
			Instance = this;
			Service = new FileSaveService();
		}

		public Task SaveAsync(GameState state) => Service.SaveAsync(state);
		public Task<(bool ok, GameState state)> LoadAsync() => Service.LoadAsync();
	}
}


