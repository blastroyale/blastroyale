using Backend.Game;
using FirstLight.Game.Logic;

namespace GameLogicService.Models
{
	public class GameLogicExecutionContext
	{
		public ServerPlayerDataProvider PlayerData;
		public IGameLogic GameLogic;
	}
}