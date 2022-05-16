using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's rewards
	/// </summary>
	public interface IRewardDataProvider
	{
		/// <summary>
		/// Requests the list of rewards in buffer to be awarded to the player
		/// </summary>
		IReadOnlyList<RewardData> UnclaimedRewards { get; }

		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="matchData"/> performance from a game completed
		/// </summary>
		Dictionary<GameId, int> GetMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit, ResourcePoolConfig csPoolConfig);
	}

	/// <inheritdoc />
	public interface IRewardLogic : IRewardDataProvider
	{
		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="matchData"/> performance from a game completed
		/// </summary>
		List<RewardData> GiveMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit, ResourcePoolConfig csPoolConfig);
		
		/// <summary>
		/// DEBUG Generate a list of rewards based on the players <paramref name="matchData"/> performance from a game completed
		/// </summary>
		List<RewardData> DebugGiveMatchRewards(ResourcePoolConfig csPoolConfig);

		/// <summary>
		/// Collects all the unclaimed rewards in the player's inventory
		/// </summary>
		List<RewardData> CollectUnclaimedRewards();

		/// <summary>
		/// Awards the given <paramref name="reward"/> to the player
		/// </summary>
		RewardData GiveReward(RewardData reward);
		
		/// <summary>
		/// Tries to withdraw and award a currency/resource from a given <paramref name="pool"/>
		/// </summary>
		/// <returns>Amount of currency/resource that was awarded from resource pool.</returns>
		ulong AwardFromResourcePool(ulong amountToAward, GameId pool, ResourcePoolConfig poolConfig);
	}

	/// <inheritdoc cref="IRewardLogic"/>
	public class RewardLogic : AbstractBaseLogic<PlayerData>, IRewardLogic
	{
		/// <inheritdoc />
		public IReadOnlyList<RewardData> UnclaimedRewards => Data.UncollectedRewards;

		public RewardLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public Dictionary<GameId, int> GetMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit, ResourcePoolConfig csPoolConfig)
		{
			var rewards = new Dictionary<GameId, int>();
			var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var mapConfig = GameLogic.ConfigsProvider.GetConfig<MapConfig>(matchData.MapId);
			var rankValue = mapConfig.PlayersLimit + 1 - matchData.PlayerRank;
			var fragValue = Math.Max(0, matchData.Data.PlayersKilledCount - matchData.Data.DeathCount * gameConfig.DeathSignificance.AsFloat);
			var currency = Math.Ceiling(gameConfig.CoinsPerRank * rankValue + gameConfig.CoinsPerFragDeathRatio.AsFloat * fragValue);

			if (currency > 0)
			{
				// TODO - '100' is hard coded. Replace with NFT awarding calculations when ready
				rewards.Add(GameId.CS, (int) AwardFromResourcePool((ulong)currency,GameId.CS, csPoolConfig));
			}

			return rewards;
		}

		/// <inheritdoc />
		public List<RewardData> GiveMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit, ResourcePoolConfig csPoolConfig)
		{
			var rewards = GetMatchRewards(matchData, didPlayerQuit, csPoolConfig);
			var rewardsList = new List<RewardData>();

			foreach (var reward in rewards)
			{
				var rewardData = new RewardData(reward.Key, reward.Value);
				
				rewardsList.Add(rewardData);
				Data.UncollectedRewards.Add(rewardData);
			}

			return rewardsList;
		}

		public List<RewardData> DebugGiveMatchRewards(ResourcePoolConfig csPoolConfig)
		{
			var rewards = new Dictionary<GameId, int>();
			var rewardsList = new List<RewardData>();
			
			rewards.Add(GameId.CS, (int) AwardFromResourcePool(75,GameId.CS, csPoolConfig));

			foreach (var reward in rewards)
			{
				var rewardData = new RewardData(reward.Key, reward.Value);
				
				rewardsList.Add(rewardData);
				Data.UncollectedRewards.Add(rewardData);
			}

			return rewardsList;
		}

		/// <inheritdoc />
		public List<RewardData> CollectUnclaimedRewards()
		{
			var rewards = new List<RewardData>(Data.UncollectedRewards.Count);
			
			if (Data.UncollectedRewards.Count == 0)
			{
				throw new LogicException("The player does not have any rewards to collect.");
			}

			foreach (var reward in Data.UncollectedRewards)
			{
				if (reward.RewardId.IsInGroup(GameIdGroup.LootBox))
				{
					// Loot boxes are already rewarded when the game is over
					continue;
				}
				
				rewards.Add(GiveReward(reward));
			}
			
			Data.UncollectedRewards.Clear();

			return rewards;
		}

		/// <inheritdoc />
		public RewardData GiveReward(RewardData reward)
		{
			var groups = reward.RewardId.GetGroups();

			if (reward.RewardId == GameId.XP)
			{
				GameLogic.PlayerLogic.AddXp((uint) reward.Value);
			}
			else if (groups.Contains(GameIdGroup.Currency))
			{
				GameLogic.CurrencyLogic.AddCurrency(reward.RewardId, (uint) reward.Value);
			}
			else
			{
				throw new LogicException($"The reward '{reward.RewardId}' is not from a group type that is rewardable.");
			}

			return reward;
		}
		
		/// <inheritdoc />
		public ulong AwardFromResourcePool(ulong amountToAward, GameId pool, ResourcePoolConfig poolConfig)
		{
			// Check and restock pool before any currency is drawn and awarded
			GameLogic.CurrencyLogic.RestockResourcePool(GameId.CS, poolConfig);
			
			var currentPoolData = GameLogic.CurrencyLogic.ResourcePools[pool];
			ulong amountWithdrawn = currentPoolData.Withdraw(amountToAward, poolConfig);
			
			Data.ResourcePools[pool] = currentPoolData;

			return amountWithdrawn;
		}
	}
}