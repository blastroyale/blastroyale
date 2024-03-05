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
		public UniTask<Web3State> OnLoginRequested();

		/// <summary>
		/// Called when user wants to logout from web3 (e.g he wants to change accounts)
		/// </summary>
		public UniTaskVoid OnLogoutRequested();
	}
}