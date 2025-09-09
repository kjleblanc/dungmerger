using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace MergeDungeon.Core
{
	public class FileSaveService : ISaveService
	{
		private readonly string path = Path.Combine(Application.persistentDataPath, "save.json");
		private readonly string tmpPath = Path.Combine(Application.persistentDataPath, "save.tmp");

		public async Task SaveAsync(GameState state)
		{
			if (state == null) return;
			var json = JsonUtility.ToJson(state);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			await Task.Run(() => File.WriteAllText(tmpPath, json));
			await Task.Run(() => { File.Copy(tmpPath, path, true); File.Delete(tmpPath); });
		}

		public async Task<(bool ok, GameState state)> LoadAsync()
		{
			if (!File.Exists(path)) return (false, null);
			var json = await Task.Run(() => File.ReadAllText(path));
			if (string.IsNullOrEmpty(json)) return (false, null);
			var state = JsonUtility.FromJson<GameState>(json);
			if (state == null) return (false, null);
			// migrations by version can be applied here
			return (true, state);
		}

		public async Task<bool> DeleteAsync()
		{
			if (!File.Exists(path)) return true;
			await Task.Run(() => File.Delete(path));
			return true;
		}
	}
}


