using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Server.SDK.Models;

namespace FirstLightServerSDK.Services
{
	/// <summary>
	/// Responsible for syncing external inventories with game inventory
	/// <typeparam name="T">The type of items in case of BlastRoyale T = ItemData</typeparam>
	/// </summary>
	public interface IInventorySyncService<T>
	{
		Task<IReadOnlyList<T>> SyncData(ServerState state, string player);
	}
}