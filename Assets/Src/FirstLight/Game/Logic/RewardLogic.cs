using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic.RPC;
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
		IObservableListReader<RewardData> UnclaimedRewards { get; }

		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="matchData"/> performance from a game completed
		/// </summary>
		List<RewardData> CalculateMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit);
	}

	/// <inheritdoc />
	public interface IRewardLogic : IRewardDataProvider
	{
		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="matchData"/> performance from a game completed
		/// </summary>
		List<RewardData> GiveMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit);

		/// <summary>
		/// Collects all the unclaimed rewards in the player's inventory
		/// </summary>
		List<RewardData> ClaimUncollectedRewards();
	}

	/// <inheritdoc cref="IRewardLogic"/>
	public class RewardLogic : AbstractBaseLogic<PlayerData>, IRewardLogic, IGameLogicInitializer
	{
		private IObservableList<RewardData> _unclaimedRewards;
		
		/// <inheritdoc />
		public IObservableListReader<RewardData> UnclaimedRewards => _unclaimedRewards;
		
		private QuantumGameConfig GameConfig => GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();

		public RewardLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			_unclaimedRewards = new ObservableList<RewardData>(Data.UncollectedRewards);
		}
#pragma warning disable 0162
		/// <inheritdoc />
		public List<RewardData> CalculateMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit)
		{
			var mapConfig = GameLogic.ConfigsProvider.GetConfig<QuantumMapConfig>(matchData.MapId);
			var rewards = new List<RewardData>();
			
			// Currently, there is no plan on giving rewards on anything but BR mode
			if (mapConfig.GameMode != GameMode.BattleRoyale || didPlayerQuit)
			{
				return rewards;
			}
			
			// Always perform ordering operation on the configs.
			// If config data placement order changed in google sheet, it could silently screw up this algorithm.
			var gameModeRewardConfigs = GameLogic.ConfigsProvider
			                                     .GetConfigsList<MatchRewardConfig>()
			                                     .OrderByDescending(x => x.Placement).ToList();
			
			var rewardConfig = gameModeRewardConfigs[0];
			var rankValue = matchData.PlayerRank;
			
			foreach (var config in gameModeRewardConfigs)
			{
				if (rankValue > config.Placement)
				{
					break;
				}
				
				if (config.Placement == rankValue)
				{
					rewardConfig = config;
					break;
				}
			}

			var csRewardPair = rewardConfig.Rewards.FirstOrDefault(x => x.Key == GameId.CS);
			var csPercent = csRewardPair.Value / 100d;
			// csRewardPair.Value is the absolute percent of the max CS take that people will be awarded
			
			var csInfo = GameLogic.ResourceLogic.GetResourcePoolInfo(GameId.CS);
			var csTake = (uint) Math.Ceiling(csInfo.WinnerRewardAmount * csPercent);
			var csWithdrawn = (int) Math.Min(csInfo.CurrentAmount, csTake);
			
			if (csWithdrawn > 0)
			{
				// TODO: Uncomment when we reward CS again
				//rewards.Add(new RewardData(GameId.CS, csWithdrawn));
			}

			return rewards;
		}
#pragma warning restore 0162

#pragma warning disable 0162
		/// <inheritdoc />
		public List<RewardData> GiveMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit)
		{
			var rewards = CalculateMatchRewards(matchData, didPlayerQuit);

			foreach (var reward in rewards)
			{
				if (reward.RewardId.IsInGroup(GameIdGroup.ResourcePool))
				{
					GameLogic.ResourceLogic.WithdrawFromResourcePool(reward.RewardId, (uint) reward.Value);
				}
				
				Data.UncollectedRewards.Add(reward);
			}
			
			return rewards;
		}
#pragma warning restore 0162
		

		/// <inheritdoc />
		public List<RewardData> ClaimUncollectedRewards()
		{
			var rewards = new List<RewardData>(Data.UncollectedRewards.Count);
			
			foreach (var reward in Data.UncollectedRewards)
			{
				rewards.Add(ClaimReward(reward));
			}

			Data.UncollectedRewards.Clear();

			return rewards;
		}

		private RewardData ClaimReward(RewardData reward)
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
	}
}