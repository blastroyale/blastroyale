using System;
using System.Collections.Generic;
using System.Linq;
using DG.DemiEditor;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Services;
using FirstLight;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's currency
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
		/// Requests the current capacity of a given <paramref name="poolType"/>, based on various factors, such as amount of NFTs owned
		/// </summary>
		ulong GetCurrentPoolCapacity(GameId poolType);

		/// <summary>
		/// Requests the amount of resource restocked per interval of a given <paramref name="poolType"/>
		/// </summary>
		ulong GetPoolRestockAmountPerInterval(GameId poolType);
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
		/// Tries to restock a given pool type (restocking dependent on last restock time, current time)
		/// </summary>
		void RestockResourcePool(GameId poolType);

		/// <summary>
		/// Tries to withdraw and award a currency/resource from a given <paramref name="pool"/>
		/// </summary>
		/// <returns>Amount of currency/resource that was awarded from resource pool.</returns>
		ulong WithdrawFromResourcePool(ulong amountToAward, GameId pool);
	}

	/// <inheritdoc cref="ICurrencyLogic"/>
	public class CurrencyLogic : AbstractBaseLogic<PlayerData>, ICurrencyLogic, IGameLogicInitializer
	{
		private IObservableDictionary<GameId, ulong> _currencies;
		private IObservableDictionary<GameId, ResourcePoolData> _resourcePools;
		private AppData AppData => DataProvider.GetData<AppData>();

		/// <inheritdoc />
		public IObservableDictionaryReader<GameId, ulong> Currencies => _currencies;
		/// <inheritdoc />
		public IObservableDictionaryReader<GameId, ResourcePoolData> ResourcePools => _resourcePools;

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
		public ulong WithdrawFromResourcePool(ulong amountToAward, GameId pool)
		{
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int)GameId.CS);
			var poolData = GameLogic.CurrencyLogic.ResourcePools[pool];
			var amountWithdrawn = amountToAward > poolData.CurrentResourceAmountInPool ? poolData.CurrentResourceAmountInPool : amountToAward;

			// If withdrawing from full pool, the next restock timer needs to restarted, as opposed to ticking already.
			// When at max pool capacity, the player will see 'Storage Full' on the ResourcePoolWidget
			if (poolData.CurrentResourceAmountInPool >= poolConfig.PoolCapacity)
			{
				poolData.LastPoolRestockTime = DateTime.UtcNow;
			}

			poolData.CurrentResourceAmountInPool -= amountWithdrawn;
			
			Data.ResourcePools[pool] = poolData;

			return amountWithdrawn;
		}

		/// <inheritdoc />
		public ulong GetCurrentPoolCapacity(GameId poolType)
		{
			// To understand the calculations below better, see link. Do NOT change anything here without understanding the system completely.
			// https://firstlightgames.atlassian.net/wiki/spaces/BB/pages/1789034519/Pool+System#Taking-from-pools-setup
			//
			// This calculation can be tested by using specific equipment.
			// Get this test equipment by using SROptions.Cheats.RemoveAllEquipment, and then SROptions.Cheats.UnlockEquipmentSet
			// Test calculations for this algorithm can be found at the bottom of this spreadsheet:
			// https://docs.google.com/spreadsheets/d/1LrHGwlNi2tbb7I8xmQVNCKKbc9YgEJjYyA8EFsIFarw/edit#gid=1028779545
			
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int)poolType);
			var nftOwned = GameLogic.EquipmentLogic.Inventory.Count - 1; // -1 to omit the hammer, which is a default weapon in the inventory
			var poolCapacity = (float)0;
			
			// Utility for calculations
			var shapeMod = poolConfig.ShapeModifier;
			var scaleMult = poolConfig.ScaleMultiplier;
			var nftAssumed = 40;
			var minNftOwned = 3;
			var adjRarityCurveMod = 0.8f;
			var nftsm = nftAssumed * shapeMod;
			var poolDecreaseExp = poolConfig.PoolCapacityDecreaseExponent;
			var maxPoolDecreaseMod = poolConfig.MaxPoolCapacityDecreaseModifier;
			
			// ----- Set base pool capacity - based on player's owned NFT
			var capacityMaxCalc = MathF.Pow(nftsm, 2) - MathF.Pow((nftsm - MathF.Min(nftOwned, nftsm) + minNftOwned - 1), 2);
			var capacityNftBonus = MathF.Floor(MathF.Sqrt(MathF.Max(0, capacityMaxCalc)) * scaleMult);
			
			poolCapacity += poolConfig.PoolCapacity + capacityNftBonus;

			// ----- Increase pool capacity based on owned NFT rarity and adjectives
			var modEquipmentList = new List<Tuple<float, Equipment>>();
			var augmentedModSum = (float) 0;
			
			foreach (var nft in GameLogic.EquipmentLogic.Inventory)
			{
				if (nft.Value.GameId == GameId.Hammer)
				{
					continue;
				}
				
				var rarityConfig = GameLogic.ConfigsProvider.GetConfig<RarityDataConfig>((int)nft.Value.Rarity);
				var adjectiveConfig = GameLogic.ConfigsProvider.GetConfig<AdjectiveDataConfig>((int)nft.Value.Adjective);
				var modSum = rarityConfig.PoolCapacityModifier + adjectiveConfig.PoolCapacityModifier;
				
				modEquipmentList.Add(new Tuple<float, Equipment>(modSum,nft.Value));
			}
			
			modEquipmentList = modEquipmentList.OrderByDescending(x => x.Item1).ToList();
			var currentIndex = 1;
			
			foreach (var modSumNft in modEquipmentList)
			{
				var strength = MathF.Pow( MathF.Max( 0, 1 - MathF.Pow( currentIndex - 1, adjRarityCurveMod ) / nftAssumed ), minNftOwned );
				augmentedModSum += modSumNft.Item1 * strength;
				currentIndex++;
			}

			poolCapacity += (ulong)(poolCapacity * augmentedModSum);
			
			// ----- Decrease pool capacity based on owned NFT durability
			var totalNftDurability = (float) 0;

			foreach (var nft in GameLogic.EquipmentLogic.Inventory)
			{
				if (nft.Value.GameId == GameId.Hammer)
				{
					continue;
				}

				totalNftDurability += nft.Value.Durability / 100f;
			}
			
			var nftDurabilityAvg = totalNftDurability / nftOwned;
			var durabilityDecreaseMult = MathF.Pow(1 - nftDurabilityAvg, poolDecreaseExp) * maxPoolDecreaseMod;
			var durabilityDecrease = MathF.Floor(poolCapacity * durabilityDecreaseMult);
			
			poolCapacity -= durabilityDecrease;
			
			return (ulong)poolCapacity;
		}

		/// <inheritdoc />
		public ulong GetPoolRestockAmountPerInterval(GameId poolType)
		{
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int)poolType);
			var currentPoolCapacity = GetCurrentPoolCapacity(poolType);

			return currentPoolCapacity / (poolConfig.TotalRestockIntervalMinutes / poolConfig.RestockIntervalMinutes);
		}

		/// <inheritdoc />
		public void RestockResourcePool(GameId poolType)
		{
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int)poolType);
			var poolData = GameLogic.CurrencyLogic.ResourcePools[poolType];
			var minutesElapsedSinceLastRestock = (DateTime.UtcNow - poolData.LastPoolRestockTime).TotalMinutes;
			var amountOfRestocks = (uint) MathF.Floor((float) minutesElapsedSinceLastRestock / poolConfig.RestockIntervalMinutes);
			
			if (amountOfRestocks == 0)
			{
				return;
			}
			
			poolData.LastPoolRestockTime = poolData.LastPoolRestockTime.AddMinutes(amountOfRestocks * poolConfig.RestockIntervalMinutes);
			poolData.CurrentResourceAmountInPool += GetPoolRestockAmountPerInterval(poolType) * amountOfRestocks;

			var currentPoolCapacity = GetCurrentPoolCapacity(poolType);
			
			if (poolData.CurrentResourceAmountInPool > currentPoolCapacity)
			{
				poolData.CurrentResourceAmountInPool = currentPoolCapacity;
			}
			
			Data.ResourcePools[poolType] = poolData;
		}
	}
}