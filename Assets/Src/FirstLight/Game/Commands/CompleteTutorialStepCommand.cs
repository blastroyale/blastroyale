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
	public struct CompleteTutorialSectionCommand : IGameCommand
	{

		public TutorialSection Section;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			if (ctx.Logic.PlayerLogic().HasTutorialStep(Section))
			{
				throw new LogicException("Already completed tutorial step " + Section.ToString());
			}
			ctx.Logic.PlayerLogic().MarkTutorialStepCompleted(Section);
			var rewardItems = ctx.Logic.RewardLogic().GetRewardsFromTutorial(Section);
			ctx.Logic.RewardLogic().GiveItems(rewardItems);
		}
	}
}