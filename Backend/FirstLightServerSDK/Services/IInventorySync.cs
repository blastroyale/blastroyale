using System.Threading.Tasks;

namespace FirstLightServerSDK.Services
{
	/// <summary>
	/// Responsible for syncing external inventories with game inventory
	/// </summary>
	public interface IInventorySyncService
	{
		Task<bool> SyncData(string player);
	}
}