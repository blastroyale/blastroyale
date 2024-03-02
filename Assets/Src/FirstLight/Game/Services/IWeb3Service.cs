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

		public string? Web3Account { get; }

		public UniTask<Web3State> Web3ButtonClicked();
	}

	public class NoWeb3Service : IWeb3Service
	{
		public Web3State State { get; } = Web3State.Unavailable;
		public bool IsServiceAvailable => false;
		public string Web3Account => null;

#pragma warning disable CS0067 // unused, but its used by plugin
		public event Action<Web3State> OnStateChanged;
#pragma warning restore CS0067
		public UniTask<Web3State> Web3ButtonClicked() => throw new System.NotImplementedException();
	}
}