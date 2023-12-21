using System.Threading.Tasks;
using FirstLight.Server.SDK.Models;

namespace FirstLightServerSDK.Services
{
	/// <summary>
	/// Responsible for syncing external inventories with game inventory
	/// </summary>
	public interface IInventorySyncService
	{
		Task<bool> SyncData(ServerState state, string player);
	}
}