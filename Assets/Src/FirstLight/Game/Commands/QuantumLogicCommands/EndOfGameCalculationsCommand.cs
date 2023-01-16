using FirstLight.Game.Logic;
using System.Collections.Generic;
using System.Linq;
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
	public class EndOfGameCalculationsCommand : IQuantumCommand, IGameCommand
	{
		
		public List<QuantumPlayerMatchData> PlayersMatchData;
		public QuantumValues QuantumValues;
		public bool ValidRewardsFromFrame = true;
		public uint CollectedNFTsCount = 0;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Service;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Quantum;

		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			if (!ValidRewardsFromFrame)
			{
				return;
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
				GamePlayerCount = matchData.Count
			};
			var rewards = ctx.Logic.RewardLogic().GiveMatchRewards(rewardSource, CollectedNFTsCount, out var trophyChange);

			ctx.Services.MessageBrokerService().Publish(new GameCompletedRewardsMessage
			{
				Rewards = rewards,
				TrophiesChange = trophyChange,
				TrophiesBeforeChange = trophiesBeforeChange
			});
		}

		public void FromFrame(Frame frame, QuantumValues quantumValues)
		{
			var gameContainer = frame.GetSingleton<GameContainer>();
			var playerEntity = gameContainer.PlayersData[quantumValues.ExecutingPlayer].Entity;
			var collectedEquipment = frame.Get<PlayerCharacter>(playerEntity).Gear;
			var gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			var nftLoadout = gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.NftOnly);
			var collectedNftsCount = 0u;
			
			// We count how many NFTs from their loadout a player has collected in a match
			for (var i = 0; i < collectedEquipment.Length; i++)
			{
				for (var j = 0; i < nftLoadout.Count; j++)
				{
					if (collectedEquipment[i].GameId == nftLoadout[j].Equipment.GameId)
					{
						collectedNftsCount++;
						break;
					}
				}
			}
			
			PlayersMatchData = gameContainer.GetPlayersMatchData(frame, out _);
			QuantumValues = quantumValues;
			CollectedNFTsCount = collectedNftsCount;

			if (!frame.Context.GameModeConfig.AllowEarlyRewards && !gameContainer.IsGameCompleted &&
				!gameContainer.IsGameOver)
			{
				ValidRewardsFromFrame = false;
			}
		}
	}
}
