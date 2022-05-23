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
		/// Re-calculates pool capacity, based on various factors, such as amount of NFTs owned
		/// </summary>
		ulong GetCurrentPoolCapacity(GameId poolType);
		
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
			var poolConfig = GameLogic.ConfigsProvider.GetConfig<ResourcePoolConfig>((int)poolType);
			var poolData = GameLogic.CurrencyLogic.ResourcePools[poolType];
			var nftOwned = GameLogic.EquipmentLogic.Inventory.Count;
			var nftEquipped = GameLogic.EquipmentLogic.EquippedItems.Count;
			
			// Utility for calculations
			var SHAPE_MOD = poolConfig.ShapeModifier;
			var SCALE_MULT = poolConfig.ScaleMultiplier;
			var NFT_ASSUMED = 40;
			var MIN_NFT = 3;
			var ADJ_RARITY_CURVE_MOD = 0.8f;
			var NFTSM = NFT_ASSUMED * SHAPE_MOD;
			var MIN_NFTMINEQUP = MathF.Min(nftOwned,NFTSM) + (MIN_NFT - 1);
			
			// Calculate base pool capacity - based on player's owned NFT
			var capacityBaseCalc = (float)Math.Pow(NFTSM,2) - (NFTSM - MathF.Pow(MIN_NFTMINEQUP,2));
			var capacityNftBonus = (ulong) MathF.Floor(MathF.Sqrt(MathF.Max(0, capacityBaseCalc))) * SCALE_MULT;
			var poolCapacity = poolConfig.PoolCapacity + capacityNftBonus;

			// Increase pool capacity based on owned NFT rarity and adjectives
			var capacityAdjRarityMult = 0;

			var modSumList = new List<Tuple<float, Equipment>>();
			
			foreach (var nft in GameLogic.EquipmentLogic.Inventory)
			{
				var rarityConfig = GameLogic.ConfigsProvider.GetConfig<RarityDataConfig>((int)nft.Value.Rarity);
				var adjectiveConfig = GameLogic.ConfigsProvider.GetConfig<AdjectiveDataConfig>((int)nft.Value.Adjective);
				var modSum = rarityConfig.PoolCapacityModifier + adjectiveConfig.PoolCapacityModifier;
				
				modSumList.Add(new Tuple<float, Equipment>(modSum,nft.Value));
			}

			modSumList = modSumList.OrderByDescending(x => x.Item1).ToList();
			
			

			poolCapacity = poolCapacity + (poolCapacity * capacityAdjRarityMult);
		

			Data.ResourcePools[poolType] = poolData;
			return 0;
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
			poolData.CurrentResourceAmountInPool += poolConfig.RestockPerInterval * amountOfRestocks;
			
			if (poolData.CurrentResourceAmountInPool > poolConfig.PoolCapacity)
			{
				poolData.CurrentResourceAmountInPool = poolConfig.PoolCapacity;
			}
			
			
			Data.ResourcePools[poolType] = poolData;
		}
	}
}