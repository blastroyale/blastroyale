using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Infos;
using FirstLight.Services;
using Quantum;
using Equipment = Quantum.Equipment;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's resources
	/// </summary>
	public interface IResourceDataProvider
	{
		/// <summary>
		/// Requests the player's resource pool data. <see cref="IObservableDictionary"/>
		/// </summary>
		IObservableDictionaryReader<GameId, ResourcePoolData> ResourcePools { get; }

		/// <summary>
		/// Requests the current <see cref="ResourcePoolInfo"/> of a given <paramref name="poolType"/>,
		/// based on various factors, such as amount of NFTs owned
		/// </summary>
		ResourcePoolInfo GetResourcePoolInfo(GameId poolType);
	}

	/// <inheritdoc />
	public interface IResourceLogic : IResourceDataProvider
	{
		/// <summary>
		/// Tries to withdraw and award a currency/resource from a given <paramref name="pool"/>
		/// </summary>
		/// <returns>Amount of currency/resource that was awarded from resource pool.</returns>
		uint WithdrawFromResourcePool(GameId pool, uint amountToAward);
	}

	/// <inheritdoc cref="IResourceLogic"/>
	public class ResourceLogic : AbstractBaseLogic<PlayerData>, IResourceLogic, IGameLogicInitializer
	{
		private IObservableDictionary<GameId, ResourcePoolData> _resourcePools;
		
		/// <inheritdoc />
		public IObservableDictionaryReader<GameId, ResourcePoolData> ResourcePools => _resourcePools;
		
		private QuantumGameConfig GameConfig => GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();

		public ResourceLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			_resourcePools = new ObservableDictionary<GameId, ResourcePoolData>(Data.ResourcePools);
		}

		/// <inheritdoc />
		public ResourcePoolInfo GetResourcePoolInfo(GameId poolType)
		{
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int) poolType);
			var capacity = GetCurrentPoolCapacity(poolType, poolConfig.UseNftData);

			if (!_resourcePools.TryGetValue(poolType, out var pool))
			{
				pool = new ResourcePoolData(poolType, capacity, DateTime.UtcNow);
				
				_resourcePools.Add(poolType, pool);
			}

			var totalRestock = poolConfig.TotalRestockIntervalMinutes / poolConfig.RestockIntervalMinutes;
			var minutesElapsedSinceLastRestock = (DateTime.UtcNow - pool.LastPoolRestockTime).TotalMinutes;
			var amountOfRestocks = (uint) Math.Floor(minutesElapsedSinceLastRestock / poolConfig.RestockIntervalMinutes);
			var restockPerInterval = (uint) Math.Floor((double) capacity / totalRestock);
			var nextRestockMinutes = (amountOfRestocks + 1) * poolConfig.RestockIntervalMinutes;
			var addAmount = amountOfRestocks * restockPerInterval;

			return new ResourcePoolInfo
			{
				Id = poolType,
				Config = poolConfig,
				PoolCapacity = capacity,
				CurrentAmount = Math.Min(pool.CurrentResourceAmountInPool + addAmount, capacity),
				WinnerRewardAmount = poolConfig.UseNftData ? GetCurrentPoolReward(poolType) : 0,
				RestockPerInterval = restockPerInterval,
				NextRestockTime = pool.LastPoolRestockTime.AddMinutes(nextRestockMinutes)
			};
		}

		/// <inheritdoc />
		public uint WithdrawFromResourcePool(GameId poolType, uint amountToAward)
		{
			var poolInfo = GetResourcePoolInfo(poolType);
			var poolData = _resourcePools[poolType];
			var amountWithdrawn = amountToAward > poolInfo.CurrentAmount ? poolInfo.CurrentAmount : amountToAward;
			var restockTime = poolInfo.NextRestockTime.AddMinutes(-poolInfo.Config.RestockIntervalMinutes);

			poolData.CurrentResourceAmountInPool = poolInfo.CurrentAmount - amountWithdrawn;
			poolData.LastPoolRestockTime = poolInfo.IsFull ? DateTime.UtcNow : restockTime;
			
			_resourcePools[poolType] = poolData;

			return amountWithdrawn;
		}

		private uint GetCurrentPoolCapacity(GameId poolType, bool useNftData)
		{
			// To understand the calculations below better, see link. Do NOT change the calculations here without understanding the system completely.
			// https://firstlightgames.atlassian.net/wiki/spaces/BB/pages/1789034519/Pool+System#Taking-from-pools-setup
			//
			// This calculation can be tested by using specific equipment.
			// Get this test equipment by using SROptions.Cheats.RemoveAllEquipment, and then SROptions.Cheats.UnlockEquipmentSet
			// Test calculations for this algorithm can be found at the bottom of this spreadsheet:
			// https://docs.google.com/spreadsheets/d/1LrHGwlNi2tbb7I8xmQVNCKKbc9YgEJjYyA8EFsIFarw/edit#gid=1028779545

			var inventory = GameLogic.EquipmentLogic.GetInventoryEquipmentInfo(EquipmentFilter.NftOnly);
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int)poolType);

			if (!useNftData)
			{
				return poolConfig.PoolCapacity;
			}
			
			var nftOwned = inventory.Count;
			var poolCapacity = (double) 0;
			var shapeMod = (double) poolConfig.ShapeModifier;
			var scaleMult = (double) poolConfig.ScaleMultiplier;
			var nftAssumed = GameConfig.NftAssumedOwned;
			var minNftOwned = GameConfig.MinNftForEarnings;
			var nftsm = nftAssumed * shapeMod;
			var poolDecreaseExp = (double) poolConfig.PoolCapacityDecreaseExponent;
			var maxPoolDecreaseMod = (double) poolConfig.MaxPoolCapacityDecreaseModifier;
			var poolCapacityTrophiesMod = (double) poolConfig.PoolCapacityTrophiesModifier;

			// ----- Set base pool capacity - based on player's owned NFT
			var capacityMaxCalc = Math.Pow(nftsm, 2) - Math.Pow((nftsm - Math.Min(nftOwned, nftsm) + minNftOwned - 1), 2);
			var capacityNftBonus = Math.Floor(Math.Sqrt(Math.Max(0, capacityMaxCalc)) * scaleMult);
			
			poolCapacity += poolConfig.PoolCapacity + capacityNftBonus;

			// ----- Increase pool capacity based on owned NFT rarity and adjectives
			poolCapacity += poolCapacity * inventory.GetAugmentedModSum(GameConfig, CapacityModSumCalculation);
			
			// ----- Increase pool capacity based on current player Trophies
			var trophiesIncrease = poolCapacity * (GameLogic.PlayerLogic.Trophies.Value  / poolCapacityTrophiesMod);
			poolCapacity += Math.Round(trophiesIncrease);
			
			// ----- Decrease pool capacity based on owned NFT durability
			var totalDurability = inventory.GetAvgDurability(out var maxDurability);
			var nftDurabilityPercent = (double)totalDurability / maxDurability;
			var durabilityDecreaseMult = Math.Pow(1 - nftDurabilityPercent, poolDecreaseExp) * maxPoolDecreaseMod;
			var durabilityDecrease = Math.Floor(poolCapacity * durabilityDecreaseMult);
			
			poolCapacity -= durabilityDecrease;
			
			return (uint)poolCapacity;
		}

		private uint GetCurrentPoolReward(GameId poolId)
		{
			// To understand the calculations below better, see link. Do NOT change the calculations here without understanding the system completely.
			// https://firstlightgames.atlassian.net/wiki/spaces/BB/pages/1789034519/Pool+System#Taking-from-pools-setup

			var loadoutItems = GameLogic.EquipmentLogic.GetLoadoutEquipmentInfo(EquipmentFilter.NftOnly);
			var nftsEquipped = (uint) loadoutItems.Count;
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int)poolId);
			var maxTake = poolConfig.BaseMaxTake;
			var takeDecreaseMod = (double) poolConfig.MaxTakeDecreaseModifier;
			var takeDecreaseExp = (double) poolConfig.TakeDecreaseExponent;

			// ----- Increase CS max take per grade of equipped NFTs
			var augmentedModSum = loadoutItems.GetAugmentedModSum(GameConfig, RewardModSumCalculation);
			
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

		private double RewardModSumCalculation(EquipmentInfo info)
		{
			var gradeConfig = GameLogic.ConfigsProvider.GetConfig<GradeDataConfig>((int)info.Equipment.Grade);
			
			return (double) gradeConfig.PoolIncreaseModifier;
		}

		private double CapacityModSumCalculation(EquipmentInfo info)
		{
			var rarityConfig = GameLogic.ConfigsProvider.GetConfig<RarityDataConfig>((int)info.Equipment.Rarity);
			var adjectiveConfig = GameLogic.ConfigsProvider.GetConfig<AdjectiveDataConfig>((int)info.Equipment.Adjective);
			
			return rarityConfig.PoolCapacityModifier.AsDouble + adjectiveConfig.PoolCapacityModifier.AsDouble;
		}
	}
}