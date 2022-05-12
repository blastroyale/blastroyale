using System;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Services;
using FirstLight;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using Quantum;

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
		/// Tries to restock a resource pool of given <paramref name="pool"/> ID.
		/// </summary>
		/// /// <exception cref="LogicException">
		/// Thrown when the given <paramref name="pool"/> is not ready to be restocked. 
		/// </exception>
		ResourcePoolData RestockResourcePool(GameId pool, ResourcePoolConfig poolConfig, bool forceRestock);

		/// <summary>
		/// Tries to withdraw and award a currency/resource from a given <paramref name="pool"/>
		/// </summary>
		/// <returns>Amount of currency/resource that was awarded from resource pool.</returns>
		ulong AwardFromResourcePool(ulong amountToAward, GameId pool, ResourcePoolConfig poolConfig);
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
		public ResourcePoolData RestockResourcePool(GameId pool, ResourcePoolConfig poolConfig, bool forceRestock)
		{
			var currentPoolData = ResourcePools[pool];

			if (!forceRestock && DateTime.UtcNow < currentPoolData.LastPoolRestockTime)
			{
				throw new LogicException($"Resource pool of {pool} is not ready to be restocked. " +
				                         $"Current UTC time: {DateTime.UtcNow}. " +
				                         $"Restock time: {currentPoolData.LastPoolRestockTime.AddMinutes(poolConfig.RestockIntervalMinutes)}.");
			}

			currentPoolData.Restock(poolConfig, forceRestock);
			Data.ResourcePools[pool] = currentPoolData;

			return currentPoolData;
		}

		/// <inheritdoc />
		public ulong AwardFromResourcePool(ulong amountToAward, GameId pool, ResourcePoolConfig poolConfig)
		{
			var currentPoolData = ResourcePools[pool];
			ulong amountWithdrawn = currentPoolData.Withdraw(amountToAward, poolConfig);
			
			Data.ResourcePools[pool] = currentPoolData;
			AddCurrency(pool, amountWithdrawn);

			return amountWithdrawn;
		}
	}
}