using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Services;

namespace FirstLight.Game.Commands
{
	public struct RedeemBPPCommand : IGameCommand
	{
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			if (gameLogic.BattlePassLogic.RedeemBPP(out var rewards, out var newLevel))
			{
				gameLogic.MessageBrokerService.Publish(new BattlePassLevelUpMessage
				{
					Rewards = rewards,
					newLevel = newLevel
				});
			}
		}
	}
}