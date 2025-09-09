using System.Threading.Tasks;

namespace MergeDungeon.Core
{
	public interface ISaveService
	{
		Task SaveAsync(GameState state);
		Task<(bool ok, GameState state)> LoadAsync();
		Task<bool> DeleteAsync();
	}
}


