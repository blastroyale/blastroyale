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
	public interface IWeb3Service : IExternalService
	{
		event Action<Web3State> OnStateChanged;
		public Web3State State { get; }
		public string Web3Account { get; }
		public UniTask<Web3State> LoginRequested();
		public UniTaskVoid LogoutRequested();
	}
}