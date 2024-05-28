using FirstLight.SDK.Services;
using Quantum;

namespace FirstLight.Game.Messages
{
	/// <summary>
	/// Triggered (MANUALLY!) when a currency change event happens.
	/// </summary>
	public struct CurrencyChangedMessage : IMessage
	{
		public GameId Id;
		public int Change;
		public string Category;
		public ulong NewValue;
	}
}