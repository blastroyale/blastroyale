using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Initializes battle pass data and also claim previous season rewards
	/// </summary>
	public struct InitializeBattlepassSeasonCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;
		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Initialization;

		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			ctx.Logic.BattlePassLogic().InitializeSeason();

			var rewards = ctx.Logic.BattlePassLogic().GetUncollectedRewardsFromPreviousSeasons();
			var rewardItems = ctx.Logic.RewardLogic().CreateItemsFromConfigs(rewards);
			ctx.Logic.BattlePassLogic().MarkRewardsFromPreviousSeasonsAsClaimed();
			ctx.Logic.RewardLogic().RewardToUnclaimedRewards(rewardItems);
			return UniTask.CompletedTask;
		}
	}
}