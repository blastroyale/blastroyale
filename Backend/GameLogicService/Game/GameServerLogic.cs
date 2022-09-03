using FirstLight;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;

namespace Backend.Game
{
	public class GameServerLogic : GameLogic // WIP
	{
		public GameServerLogic(IConfigsProvider cfg, IDataProvider data) : base(
		                                                                        new MessageBrokerService(),
		                                                                        new ServerTime(), 
		                                                                        data,
		                                                                        cfg, 
		                                                                        new ServerAudio()
		                                                                       )
		{
		}
	}
}

