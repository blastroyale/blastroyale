using System;
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
		IReadOnlyList<RewardData> UnclaimedRewards { get; }

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

		/// <summary>
		/// Awards the given <paramref name="reward"/> to the player
		/// </summary>
		RewardData ClaimReward(RewardData reward);
	}

	/// <inheritdoc cref="IRewardLogic"/>
	public class RewardLogic : AbstractBaseLogic<PlayerData>, IRewardLogic
	{
		/// <inheritdoc />
		public IReadOnlyList<RewardData> UnclaimedRewards => Data.UncollectedRewards;
		
		private QuantumGameConfig GameConfig => GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();

		public RewardLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

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
			
			var csPool = GameLogic.CurrencyLogic.GetResourcePoolInfo(GameId.CS);
			var csTake = (uint) Math.Ceiling(GetMatchRewardPoolTake(GameId.CS) * csPercent);
			var csWithdrawn = (int) Math.Min(csPool.CurrentAmount, csTake);
			
			if (csWithdrawn > 0)
			{
				rewards.Add(new RewardData(GameId.CS, csWithdrawn));
			}

			return rewards;
		}

		/// <inheritdoc />
		public List<RewardData> GiveMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit)
		{
			var rewards = CalculateMatchRewards(matchData, didPlayerQuit);
			var poolRewards = rewards.FindAll(reward => reward.RewardId.IsInGroup(GameIdGroup.ResourcePool));

			foreach (var reward in poolRewards)
			{
				GameLogic.CurrencyLogic.WithdrawFromResourcePool(reward.RewardId, (uint) reward.Value);
			}
			
			Data.UncollectedRewards.AddRange(rewards);

			return rewards;
		}

		/// <inheritdoc />
		public List<RewardData> ClaimUncollectedRewards()
		{
			var rewards = new List<RewardData>(Data.UncollectedRewards.Count);
			
			if (Data.UncollectedRewards.Count == 0)
			{
				throw new LogicException("The player does not have any rewards to collect.");
			}
			
			foreach (var reward in Data.UncollectedRewards)
			{
				rewards.Add(ClaimReward(reward));
			}

			Data.UncollectedRewards.Clear();

			return rewards;
		}

		/// <inheritdoc />
		public RewardData ClaimReward(RewardData reward)
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

		private uint GetMatchRewardPoolTake(GameId poolId)
		{
			// To understand the calculations below better, see link. Do NOT change the calculations here without understanding the system completely.
			// https://firstlightgames.atlassian.net/wiki/spaces/BB/pages/1789034519/Pool+System#Taking-from-pools-setup

			var loadoutItems = GameLogic.EquipmentLogic.GetLoadoutEquipmentInfo();
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int)poolId);
			var maxTake = poolConfig.BaseMaxTake;
			var takeDecreaseMod = (double) poolConfig.MaxTakeDecreaseModifier;
			var takeDecreaseExp = (double) poolConfig.TakeDecreaseExponent;
			var nftsEquipped = (uint) GameLogic.EquipmentLogic.Loadout.Count;
			
			// ----- Increase CS max take per grade of equipped NFTs
			var augmentedModSum = loadoutItems.GetAugmentedModSum(GameConfig, ModSumCalculation);
			
			maxTake += (uint) Math.Round(maxTake * augmentedModSum);
			
			// ----- Decrease CS max take based on equipped NFT durability
			var totalDurability = loadoutItems.GetAvgDurability(out var maxDurability);
			var nftDurabilityPercent = (double)totalDurability / maxDurability;
			var durabilityDecreaseMult = Math.Pow(1 - nftDurabilityPercent, takeDecreaseExp) * takeDecreaseMod;
			
			maxTake -= (uint) Math.Round(maxTake * durabilityDecreaseMult);
			
			// ----- Get take based on amount of NFTs equipped
			var csTake = (uint) Math.Ceiling((double) maxTake / Equipment.EquipmentSlots.Count * nftsEquipped);
			
			// NOTE: Final take should afterwards be modified by placement in match
			
			return csTake;
		}

		private double ModSumCalculation(EquipmentInfo info)
		{
			var gradeConfig = GameLogic.ConfigsProvider.GetConfig<GradeDataConfig>((int)info.Equipment.Grade);
			
			return (double) gradeConfig.PoolIncreaseModifier;
		}
	}
}