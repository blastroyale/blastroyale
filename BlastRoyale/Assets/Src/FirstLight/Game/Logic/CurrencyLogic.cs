using FirstLight.Game.Data;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK.Models;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's currencies
	/// </summary>
	public interface ICurrencyDataProvider
	{
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
	}

	/// <inheritdoc cref="ICurrencyLogic"/>
	public class CurrencyLogic : AbstractBaseLogic<PlayerData>, ICurrencyLogic, IGameLogicInitializer
	{
		private IObservableDictionary<GameId, ulong> _currencies;

		/// <inheritdoc />
		public IObservableDictionaryReader<GameId, ulong> Currencies => _currencies;

		public CurrencyLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public void Init()
		{
			var defaultValues = new PlayerData().Currencies;
			
			_currencies = new ObservableDictionary<GameId, ulong>(Data.Currencies);

			foreach (var pair in defaultValues)
			{
				if (!_currencies.ContainsKey(pair.Key))
				{
					_currencies.Add(pair.Key, pair.Value);
				}
			}
		}

		public void ReInit()
		{
			var defaultValues = new PlayerData().Currencies;
			
			{
				var listeners = _currencies.GetObservers();
				_currencies = new ObservableDictionary<GameId, ulong>(Data.Currencies);
				_currencies.AddObservers(listeners);
			}
			
			foreach (var pair in defaultValues)
			{
				if (!_currencies.ContainsKey(pair.Key))
				{
					_currencies.Add(pair.Key, pair.Value);
				}
			}
			
			_currencies.InvokeUpdate();
		}

		/// <inheritdoc />
		public ulong GetCurrencyAmount(GameId currency)
		{
			if (!currency.IsInGroup(GameIdGroup.Currency))
			{
				throw new LogicException($"The given game Id {currency.ToString()} is not of {GameIdGroup.Currency} type");
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
					LogicException($"The player needs more {amount.ToString()} of {currency.ToString()} for the transaction " +
					               $"and only has {oldAmount.ToString()}");
			}

			_currencies[currency] = oldAmount - amount;
		}
	}
}