using FirstLight.Game.Logic;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;

namespace Backend.Game
{
	public class GameServerLogic : GameLogic // WIP
	{
		public GameServerLogic(IConfigsProvider cfg, IDataProvider data, IMessageBrokerService msgBroker) : base(
			msgBroker,
			new ServerTime(),
			data,
			cfg
		)
		{
		}
	}
}