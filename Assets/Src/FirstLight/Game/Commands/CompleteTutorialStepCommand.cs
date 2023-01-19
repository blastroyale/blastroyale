using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Services;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Completes a given tutorial step.
	/// Will grant rewards of the given step.
	/// </summary>
	public struct CompleteTutorialStepCommand : IGameCommand
	{

		public TutorialStep Step;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			if (ctx.Logic.PlayerLogic().HasTutorialStep(Step))
			{
				throw new LogicException("Already completed tutorial step " + Step.ToString());
			}
			ctx.Logic.PlayerLogic().MarkTutorialStepCompleted(Step);
			var rewardItems = ctx.Logic.RewardLogic().GetRewardsFromTutorial(Step);
			ctx.Logic.RewardLogic().GiveItems(rewardItems);
		}
	}
}