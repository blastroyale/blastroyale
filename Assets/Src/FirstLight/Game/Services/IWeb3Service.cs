namespace FirstLight.Game.Services
{
	public enum Web3State
	{
		Unavailable, Initializing, Reading, Ready,
	}

	/// <summary>
	/// Service to consume web3 specific features
	/// </summary>
	public interface IWeb3Service : IExternalService
	{
		public Web3State State { get; }

		public bool OpenWeb3();
	}

	public class NoWeb3Service : IWeb3Service
	{
		public Web3State State { get; } = Web3State.Unavailable;
		public bool IsServiceAvailable => false;
		public bool OpenWeb3() => throw new System.NotImplementedException();
	}
}