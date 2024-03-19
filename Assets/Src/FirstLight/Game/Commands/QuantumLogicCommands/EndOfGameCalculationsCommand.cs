using FirstLight.Game.Logic;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Updates player trophies, restocks resource pools, and gives end-of-match rewards
	/// </summary>
	public unsafe class EndOfGameCalculationsCommand : IQuantumCommand, IGameCommand
	{
		public Dictionary<GameId, ushort> EarnedGameItems;
		public List<QuantumPlayerMatchData> PlayersMatchData;
		public QuantumValues QuantumValues;
		public bool ValidRewardsFromFrame = true;
		public bool RunningTutorialMode = false;
		public uint TeamSize;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Service;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Quantum;

		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			if (!ValidRewardsFromFrame || RunningTutorialMode)
			{
				return UniTask.CompletedTask;
			}
			
			var matchData = PlayersMatchData;
			var trophiesBeforeChange = ctx.Logic.PlayerLogic().Trophies.Value;
			var matchType = QuantumValues.MatchType;
			var rewardSource = new RewardSource()
			{
				MatchData = matchData,
				ExecutingPlayer = QuantumValues.ExecutingPlayer,
				MatchType = matchType,
				DidPlayerQuit = false,
				GamePlayerCount = matchData.Count,
				AllowedRewards = QuantumValues.AllowedRewards,
				CollectedItems = EarnedGameItems ?? new ()
			};
			
			var rewards = ctx.Logic.RewardLogic().GiveMatchRewards(rewardSource, out var trophyChange);

			ctx.Services.MessageBrokerService().Publish(new GameCompletedRewardsMessage
			{
				Rewards = rewards,
				TrophiesChange = trophyChange,
				TrophiesBeforeChange = trophiesBeforeChange
			});
			return UniTask.CompletedTask;
		}

		public void FromFrame(Frame frame, QuantumValues quantumValues)
		{
			var gameContainer = frame.Unsafe.GetPointerSingleton<GameContainer>();
			PlayersMatchData = gameContainer->GeneratePlayersMatchData(frame, out _, out _);
			
			QuantumValues = quantumValues;
			TeamSize = frame.Context.GameModeConfig.MaxPlayersInTeam;

			var executingData = PlayersMatchData[QuantumValues.ExecutingPlayer];
			var items = frame.ResolveDictionary(executingData.Data.CollectedMetaItems);
			EarnedGameItems = new Dictionary<GameId, ushort>();
			foreach (var kp in items)
			{
				EarnedGameItems[kp.Key] = kp.Value;
			}
			// TODO: Find better way to determine tutorial mode. GameConstants ID perhaps? Something that backend has access to
			RunningTutorialMode = frame.Context.GameModeConfig.Id.Contains("Tutorial");
				
			if (!frame.Context.GameModeConfig.AllowEarlyRewards && !gameContainer.IsGameCompleted &&
				!gameContainer.IsGameOver)
			{
				ValidRewardsFromFrame = false;
			}
		}
	}
}
