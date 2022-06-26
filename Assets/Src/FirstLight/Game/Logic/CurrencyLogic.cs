using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Services;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic.RPC;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's resources
	/// </summary>
	public interface ICurrencyDataProvider
	{
		/// <summary>
		/// Requests the player's resource pool data. <see cref="IObservableDictionary"/>
		/// </summary>
		IObservableDictionaryReader<GameId, ResourcePoolData> ResourcePools { get; }
		
		/// <summary>
		/// Requests the player's <seealso cref="GameIdGroup.Currency"/> <see cref="IObservableDictionary"/>
		/// </summary>
		IObservableDictionaryReader<GameId, ulong> Currencies { get; }

		/// <summary>
		/// Requests the player's <seealso cref="GameIdGroup.Currency"/> amount of the given <paramref name="currency"/>.
		/// If the player has no currency of the given type, it will add it with 0 quantity to the player saved data
		/// </summary>
		ulong GetCurrencyAmount(GameId currency);

		/// <summary>
		/// Requests the current <see cref="ResourcePoolInfo"/> of a given <paramref name="poolType"/>,
		/// based on various factors, such as amount of NFTs owned
		/// </summary>
		ResourcePoolInfo GetResourcePoolInfo(GameId poolType);
	}

	/// <inheritdoc />
	public interface ICurrencyLogic : ICurrencyDataProvider
	{
		/// <summary>
		/// Adds the given <paramref name="amount"/> to the current <paramref name="currency"/> wallet amount
		/// </summary>
		/// <exception cref="LogicException">
		/// Thrown when the given <paramref name="currency"/> is not part of the <seealso cref="GameIdGroup.Currency"/> group
		/// </exception>
		void AddCurrency(GameId currency, ulong amount);

		/// <summary>
		/// Deducts the given <paramref name="amount"/> from the current <paramref name="currency"/> wallet amount
		/// </summary>
		/// <exception cref="LogicException">
		/// Thrown when the given <paramref name="currency"/> is not part of the <seealso cref="GameIdGroup.Currency"/> group
		/// or if the given <paramref name="amount"/> is higher than the current amount in the player's wallet
		/// </exception>
		void DeductCurrency(GameId currency, ulong amount);

		/// <summary>
		/// Tries to withdraw and award a currency/resource from a given <paramref name="pool"/>
		/// </summary>
		/// <returns>Amount of currency/resource that was awarded from resource pool.</returns>
		uint WithdrawFromResourcePool(GameId pool, uint amountToAward);
	}

	/// <inheritdoc cref="ICurrencyLogic"/>
	public class CurrencyLogic : AbstractBaseLogic<PlayerData>, ICurrencyLogic, IGameLogicInitializer
	{
		private IObservableDictionary<GameId, ulong> _currencies;
		private IObservableDictionary<GameId, ResourcePoolData> _resourcePools;

		/// <inheritdoc />
		public IObservableDictionaryReader<GameId, ulong> Currencies => _currencies;
		/// <inheritdoc />
		public IObservableDictionaryReader<GameId, ResourcePoolData> ResourcePools => _resourcePools;
		
		private QuantumGameConfig GameConfig => GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();

		public CurrencyLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			_currencies = new ObservableDictionary<GameId, ulong>(Data.Currencies);
			_resourcePools = new ObservableDictionary<GameId, ResourcePoolData>(Data.ResourcePools);
		}

		/// <inheritdoc />
		public ulong GetCurrencyAmount(GameId currency)
		{
			if (!currency.IsInGroup(GameIdGroup.Currency))
			{
				throw new LogicException($"The given game Id {currency} is not of {GameIdGroup.Currency} type");
			}

			if (!_currencies.TryGetValue(currency, out var amount))
			{
				amount = 0;

				_currencies.Add(currency, amount);
			}

			return amount;
		}

		/// <inheritdoc />
		public ResourcePoolInfo GetResourcePoolInfo(GameId poolType)
		{
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int) poolType);
			var capacity = GetCurrentPoolCapacity(poolType);

			if (!_resourcePools.TryGetValue(poolType, out var pool))
			{
				pool =  new ResourcePoolData(poolType, capacity, DateTime.UtcNow);
				
				_resourcePools.Add(poolType, pool);
			}
			
			var minutesElapsedSinceLastRestock = (DateTime.UtcNow - pool.LastPoolRestockTime).TotalMinutes;
			var amountOfRestocks = (uint) Math.Floor(minutesElapsedSinceLastRestock / poolConfig.RestockIntervalMinutes);
			var restockPerInterval = capacity / poolConfig.TotalRestockIntervalMinutes / poolConfig.RestockIntervalMinutes;
			var nextRestockMinutes = (amountOfRestocks + 1) * poolConfig.RestockIntervalMinutes;
			var addAmount = amountOfRestocks * restockPerInterval;

			return new ResourcePoolInfo
			{
				Id = poolType,
				PoolCapacity = capacity,
				CurrentAmount = Math.Min(pool.CurrentResourceAmountInPool + addAmount, capacity),
				RestockPerInterval = restockPerInterval,
				NextRestockTime = pool.LastPoolRestockTime.AddMinutes(nextRestockMinutes)
			};
		}

		/// <inheritdoc />
		public void AddCurrency(GameId currency, ulong amount)
		{
			var oldAmount = GetCurrencyAmount(currency);
			var newAmount = oldAmount + amount;

			_currencies[currency] = newAmount;
		}

		/// <inheritdoc />
		public void DeductCurrency(GameId currency, ulong amount)
		{
			var oldAmount = GetCurrencyAmount(currency);

			if (amount > oldAmount)
			{
				throw new
					LogicException($"The player needs more {amount.ToString()} of {currency} for the transaction " +
					               $"and only has {oldAmount.ToString()}");
			}

			_currencies[currency] = oldAmount - amount;
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

		private uint GetCurrentPoolCapacity(GameId poolType)
		{
			// To understand the calculations below better, see link. Do NOT change the calculations here without understanding the system completely.
			// https://firstlightgames.atlassian.net/wiki/spaces/BB/pages/1789034519/Pool+System#Taking-from-pools-setup
			//
			// This calculation can be tested by using specific equipment.
			// Get this test equipment by using SROptions.Cheats.RemoveAllEquipment, and then SROptions.Cheats.UnlockEquipmentSet
			// Test calculations for this algorithm can be found at the bottom of this spreadsheet:
			// https://docs.google.com/spreadsheets/d/1LrHGwlNi2tbb7I8xmQVNCKKbc9YgEJjYyA8EFsIFarw/edit#gid=1028779545

			var inventory = GameLogic.EquipmentLogic.GetInventoryEquipmentInfo();
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int)poolType);
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
			poolCapacity += poolCapacity * inventory.GetAugmentedModSum(GameConfig, ModSumCalculation);
			
			// ----- Increase pool capacity based on current player Trophies
			poolCapacity += poolCapacity * Math.Round(GameLogic.PlayerDataProvider.Trophies.Value / poolCapacityTrophiesMod);
			
			// ----- Decrease pool capacity based on owned NFT durability
			var totalDurability = inventory.GetAvgDurability(out var maxDurability);
			var nftDurabilityPercent = (double)totalDurability / maxDurability;
			var durabilityDecreaseMult = Math.Pow(1 - nftDurabilityPercent, poolDecreaseExp) * maxPoolDecreaseMod;
			var durabilityDecrease = Math.Floor(poolCapacity * durabilityDecreaseMult);
			
			poolCapacity -= durabilityDecrease;
			
			return (uint)poolCapacity;
		}

		private double ModSumCalculation(EquipmentInfo info)
		{
			var rarityConfig = GameLogic.ConfigsProvider.GetConfig<RarityDataConfig>((int)info.Equipment.Rarity);
			var adjectiveConfig = GameLogic.ConfigsProvider.GetConfig<AdjectiveDataConfig>((int)info.Equipment.Adjective);
			
			return rarityConfig.PoolCapacityModifier.AsDouble + adjectiveConfig.PoolCapacityModifier.AsDouble;
		}
	}
}