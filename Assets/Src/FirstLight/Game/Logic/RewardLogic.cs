using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon.StructWrapping;
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
		Dictionary<GameId, int> CalculateMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit);
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
			var mapConfig = GameLogic.ConfigsProvider.GetConfig<MapConfig>(matchData.MapId);
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
			var csPercent = csRewardPair.Value / 100f;
			// csRewardPair.Value is the absolute percent of the max CS take that people will be awarded

			var loadoutSlots = GameConfig.LoadoutSlots;
			var nftsEquipped = GameLogic.EquipmentLogic.Loadout.Count;
			var csMaxTake = GetMaxPoolTake(GameId.CS);
			
			// ----- Decrease take based on amount of NFTs equipped
			var csTake = MathF.Ceiling(csMaxTake / loadoutSlots * nftsEquipped);
			
			// ---- Decrease take based on placement in match
			csTake = MathF.Ceiling(csTake * csPercent);
			
			var csWithdrawn = (int) GameLogic.CurrencyLogic.WithdrawFromResourcePool((ulong) csTake, GameId.CS);
			
			if (csWithdrawn > 0)
			{
				rewards.Add(GameId.CS, csWithdrawn);
			}

			return rewards;
		}

		private float GetMaxPoolTake(GameId resourcePoolId)
		{
			// To understand the calculations below better, see link. Do NOT change the calculations here without understanding the system completely.
			// https://firstlightgames.atlassian.net/wiki/spaces/BB/pages/1789034519/Pool+System#Taking-from-pools-setup
			
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int)resourcePoolId);
			var maxTake = (float) poolConfig.BaseMaxTake;
			var nftAssumed = GameConfig.NftAssumedOwned;
			var minNftOwned = GameConfig.MinNftForEarnings;
			var adjRarityCurveMod = (float) GameConfig.AdjectiveRarityEarningsMod;
			var loadoutItems = GameLogic.EquipmentLogic.GetLoadoutItems();
			var takeDecreaseMod = poolConfig.MaxTakeDecreaseModifier;
			var takeDecreaseExp = poolConfig.TakeDecreaseExponent;
			
			// ----- Increase CS max take per grade of equipped NFTs
			var modEquipmentList = new List<Tuple<float, Equipment>>();
			var augmentedModSum = (float) 0;
			
			foreach (var nft in loadoutItems)
			{
				if (nft.GameId == GameId.Hammer)
				{
					continue;
				}
				
				var gradeConfig = GameLogic.ConfigsProvider.GetConfig<GradeDataConfig>((int)nft.Grade);
				var modSum = gradeConfig.PoolIncreaseModifier;
				
				modEquipmentList.Add(new Tuple<float, Equipment>(modSum,nft));
			}
			
			modEquipmentList = modEquipmentList.OrderByDescending(x => x.Item1).ToList();
			var currentIndex = 1;
			
			foreach (var modSumNft in modEquipmentList)
			{
				var strength = MathF.Pow( MathF.Max( 0, 1 - MathF.Pow( currentIndex - 1, adjRarityCurveMod ) / nftAssumed ), minNftOwned );
				augmentedModSum += modSumNft.Item1 * strength;
				currentIndex++;
			}
			
			maxTake += MathF.Round(maxTake * augmentedModSum);
			
			// ----- Decrease CS max take based on equipped NFT durability
			var totalNftDurability = (float) 0;
			
			foreach (var nft in loadoutItems)
			{
				totalNftDurability += nft.Durability / 100f;
			}

			var nftDurabilityAvg = totalNftDurability / loadoutItems.Length;
			var durabilityDecreaseMult = MathF.Pow(1 - nftDurabilityAvg, takeDecreaseExp) * takeDecreaseMod;
			var durabilityDecrease = MathF.Round(maxTake * durabilityDecreaseMult);
			
			maxTake -= durabilityDecrease;
			
			return maxTake;
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