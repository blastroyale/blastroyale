using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Services;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Completes a given tutorial step.
	/// Will grant rewards of the given step.
	/// </summary>
	public struct CompleteTutorialSectionCommand : IGameCommand
	{
		public TutorialSection Section;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			if (ctx.Logic.PlayerLogic().HasTutorialSection(Section))
			{
				throw new LogicException("Already completed tutorial section " + Section);
			}
			
			ctx.Logic.PlayerLogic().MarkTutorialSectionCompleted(Section);
			var rewardItems = ctx.Logic.RewardLogic().GetRewardsFromTutorial(Section);
			ctx.Logic.RewardLogic().Reward(rewardItems);
			ctx.Services.MessageBrokerService().Publish(new CompletedTutorialSectionMessage(){Section = Section});
			return UniTask.CompletedTask;
		}
	}
}