using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;

namespace FirstLightServerSDK.Modules
{
	public interface IDataSynchronizer
	{
		void RegisterSync(IDataSync sync);
	}

}