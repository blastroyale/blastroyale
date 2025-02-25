using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Completes a given tutorial step.
	/// Will grant rewards of the given step.
	/// </summary>
	public struct CompleteTutorialSectionCommand : IGameCommand
	{
		public TutorialSection[] Sections;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			foreach (var tutorialSection in Sections)
			{
				if (!ctx.Logic.PlayerLogic().HasTutorialSection(tutorialSection))
				{
					ctx.Logic.PlayerLogic().MarkTutorialSectionCompleted(tutorialSection);

					var rewardItems = ctx.Logic.RewardLogic().GetRewardsFromTutorial(tutorialSection);
					ctx.Logic.RewardLogic().Reward(rewardItems);
					ctx.Services.MessageBrokerService().Publish(new CompletedTutorialSectionMessage() {Section = tutorialSection});
				}
			}

			return UniTask.CompletedTask;
		}
	}
}