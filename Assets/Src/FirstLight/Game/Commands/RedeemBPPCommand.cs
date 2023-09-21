using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Award the BPP to the player and if the player reaches a new level, gives automatically the reward
	/// </summary>
	public struct RedeemBPPCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public void Execute(CommandExecutionContext ctx)
		{
			if (ctx.Logic.BattlePassLogic().RedeemBPP(out var rewards, out var newLevel))
			{
				ctx.Services.MessageBrokerService().Publish(new BattlePassLevelUpMessage
				{
					Rewards = rewards,
					newLevel = newLevel
				});
				if (ctx.Logic.BattlePassLogic().IsTutorial())
				{
					if (newLevel >= ctx.Logic.BattlePassLogic().MaxLevel)
					{
						ctx.Logic.PlayerLogic().MarkTutorialSectionCompleted(TutorialSection.TUTORIAL_BP);
						ctx.Logic.BattlePassLogic().Reset();
						ctx.Services.MessageBrokerService().Publish(new TutorialBattlePassCompleted());
					}
				}
			}
		}
	}
}