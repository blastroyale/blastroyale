using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Infos;
using FirstLight.Services;
using Photon.Deterministic;
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
		Dictionary<GameId, int> CalculateMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit);

		/// <summary>
		/// Get amount of pooled resource that can be withdrawn based on currently equipped NFTs for a given <paramref name="poolId"/>
		/// </summary>
		public uint GetMatchRewardPoolTake(GameId poolId);
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
		public Dictionary<GameId, int> CalculateMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit)
		{
			var mapConfig = GameLogic.ConfigsProvider.GetConfig<QuantumMapConfig>(matchData.MapId);
			var rewards = new Dictionary<GameId, int>();
			
			// Currently, there is no plan on giving rewards on anything but BR mode
			if (mapConfig.GameMode != GameMode.BattleRoyale)
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
			
			var csTake = (uint) Math.Ceiling(GetMatchRewardPoolTake(GameId.CS) * csPercent);
			var csWithdrawn = (int) GameLogic.CurrencyLogic.WithdrawFromResourcePool(csTake, GameId.CS);
			
			if (csWithdrawn > 0)
			{
				rewards.Add(GameId.CS, csWithdrawn);
			}

			return rewards;
		}

		/// <inheritdoc />
		public uint GetMatchRewardPoolTake(GameId poolId)
		{
			// To understand the calculations below better, see link. Do NOT change the calculations here without understanding the system completely.
			// https://firstlightgames.atlassian.net/wiki/spaces/BB/pages/1789034519/Pool+System#Taking-from-pools-setup

			var loadoutItems = GameLogic.EquipmentLogic.GetLoadoutEquipmentInfo();
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int)poolId);
			var maxTake = poolConfig.BaseMaxTake;
			var nftAssumed = GameConfig.NftAssumedOwned;
			var minNftOwned = GameConfig.MinNftForEarnings;
			var adjRarityCurveMod = (double) GameConfig.AdjectiveRarityEarningsMod;
			var takeDecreaseMod = (double) poolConfig.MaxTakeDecreaseModifier;
			var takeDecreaseExp = (double) poolConfig.TakeDecreaseExponent;
			var loadoutSlots = GameConfig.LoadoutSlots;
			var nftsEquipped = (uint) GameLogic.EquipmentLogic.Loadout.Count;
			
			// ----- Increase CS max take per grade of equipped NFTs
			var modEquipmentList = new List<Tuple<double, Equipment>>();
			var augmentedModSum = (double) 0;
			
			foreach (var nft in loadoutItems)
			{
				var gradeConfig = GameLogic.ConfigsProvider.GetConfig<GradeDataConfig>((int)nft.Equipment.Grade);
				var modSum = (double) gradeConfig.PoolIncreaseModifier;
				
				modEquipmentList.Add(new Tuple<double, Equipment>(modSum,nft.Equipment));
			}
			
			modEquipmentList = modEquipmentList.OrderByDescending(x => x.Item1).ToList();
			
			for (var i = 0; i < modEquipmentList.Count; i++)
			{
				var strength = Math.Pow(Math.Max(0, 1 - Math.Pow(i, adjRarityCurveMod) / nftAssumed), minNftOwned);
				
				augmentedModSum += modEquipmentList[i].Item1 * strength;
			}
			
			maxTake += (uint) Math.Round(maxTake * augmentedModSum);
			
			// ----- Decrease CS max take based on equipped NFT durability
			var totalDurability = loadoutItems.GetAvgDurabilty(out var maxDurability);
			var nftDurabilityPercent = (double)totalDurability / maxDurability;
			var durabilityDecreaseMult = Math.Pow(1 - nftDurabilityPercent, takeDecreaseExp) * takeDecreaseMod;
			var durabilityDecrease = Math.Round(maxTake * durabilityDecreaseMult);
			
			maxTake -= (uint)durabilityDecrease;
			
			// ----- Get take based on amount of NFTs equipped
			var csTake = (uint) FPMath.Ceiling(maxTake / loadoutSlots * nftsEquipped);
			
			// NOTE: Final take should afterwards be modified by placement in match
			
			return csTake;
		}

		/// <inheritdoc />
		public List<RewardData> GiveMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit)
		{
			var rewards = CalculateMatchRewards(matchData, didPlayerQuit);
			var rewardsList = new List<RewardData>();

			foreach (var reward in rewards)
			{
				var rewardData = new RewardData(reward.Key, reward.Value);
				
				rewardsList.Add(rewardData);
				Data.UncollectedRewards.Add(rewardData);
			}

			return rewardsList;
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
	}
}