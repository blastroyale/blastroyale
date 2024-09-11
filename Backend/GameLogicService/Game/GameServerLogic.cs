using FirstLight.Game.Logic;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLightServerSDK.Services;

namespace Backend.Game
{
	public class GameServerLogic : GameLogic // WIP
	{
		public GameServerLogic(IConfigsProvider cfg, IRemoteConfigProvider remoteConfigProvider, IDataProvider data, IMessageBrokerService msgBroker) : base(
			msgBroker,
			remoteConfigProvider,
			new ServerTime(),
			data,
			cfg
		)
		{
		}
	}
}