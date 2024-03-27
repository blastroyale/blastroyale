using Cysharp.Threading.Tasks;
using System;

namespace FirstLight.Game.Services
{
	public enum Web3State
	{
		Unavailable, Available, Authenticated,
	}

	/// <summary>
	/// Service to consume web3 specific features
	/// </summary>
	public interface IWeb3Service
	{
		/// <summary>
		/// Called whenever the web3 client provider changes its connection state
		/// </summary>
		event Action<Web3State> OnStateChanged;

		/// <summary>
		/// Current state
		/// </summary>
		public Web3State State { get; }

		/// <summary>
		/// Account identifier for this web3 provider (likely wallet address)
		/// </summary>
		public string Web3Account { get; }

		/// <summary>
		/// Called whenever user desires to login and connect to web3
		/// </summary>
		public UniTask<Web3State> RequestLogin();

		/// <summary>
		/// Called when user wants to logout from web3 (e.g he wants to change accounts)
		/// </summary>
		public UniTaskVoid RequestLogout();
	}

	public class NoWeb3 : IWeb3Service
	{
		public Web3State State => Web3State.Unavailable;

		public string Web3Account => null;

#pragma warning disable CS0067 // not used, but its used by plugins
		public event Action<Web3State> OnStateChanged;
#pragma warning restore CS0067

		public UniTask<Web3State> RequestLogin()
		{
			throw new NotImplementedException();
		}

		public UniTaskVoid RequestLogout()
		{
			throw new NotImplementedException();
		}
	}
}