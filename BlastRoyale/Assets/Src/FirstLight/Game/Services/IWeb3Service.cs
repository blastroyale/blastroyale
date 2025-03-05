using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Numerics;
using Quantum;

namespace FirstLight.Game.Services
{
	public enum Web3State
	{
		Unavailable, Available, Authenticated,
	}
	
	public interface IWeb3Currency
	{
		/// <summary>
		/// Total value of OnChain + OffChain prediction user has
		/// </summary>
		IObservableField<ulong> TotalPredicted { get; }
		
		/// <summary>
		/// Only on-chain value user has of currency
		/// </summary>
		IObservableField<ulong> OnChainValue { get; }
		
		/// <summary>
		/// Reads the current value the indexer has
		/// </summary>
		void UpdateTotalValue();
	}

	/// <summary>
	/// Service to consume web3 specific features
	/// </summary>
	public interface IWeb3Service
	{
		/// <summary>
		/// Called when state is auth for first time
		/// </summary>
		event Action OnWeb3Ready;

		/// <summary>
		/// Current state
		/// </summary>
		public Web3State GetState();
		
		/// <summary>
		/// Account identifier for this web3 provider (likely wallet address)
		/// </summary>
		public string CurrentWallet { get; }

		public IReadOnlyDictionary<GameId, IWeb3Currency> GetWeb3Currencies();

		public UniTask Transfer(GameId currency, string to, BigInteger amount);

		public void Withdrawal(GameId currency);

		public bool IsEnabled();
	}

	public class NoWeb3 : IWeb3Service
	{
#pragma warning disable CS0067 // not used, but its used by plugins
		public event Action OnWeb3Ready;
#pragma warning restore CS0067
		public Web3State State => Web3State.Unavailable;

		public Web3State GetState()
		{
			return State;
		}

		public string CurrentWallet => null;
		public IReadOnlyDictionary<GameId, IWeb3Currency> Web3Currencies { get; set; }

		public IReadOnlyDictionary<GameId, IWeb3Currency> GetWeb3Currencies()
		{
			throw new NotImplementedException();
		}

		public UniTask Transfer(GameId currency, string to, BigInteger amount)
		{
			throw new NotImplementedException();
		}

		public void Withdrawal(GameId currency)
		{
			throw new NotImplementedException();
		}

		public bool IsEnabled()
		{
			return false;
		}

		public bool CanUseWeb3()
		{
			return false;
		}

		public UniTask<bool> LinkCurrentWallet()
		{
			throw new NotImplementedException();
		}

#pragma warning disable CS0067 // not used, but its used by plugins
		public event Action<Web3State> OnStateChanged;
#pragma warning restore CS0067
	}
}