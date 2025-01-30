namespace FirstLight.Server.SDK.Events
{
	/// <summary>
	/// Called when a player purchases something in shop
	/// </summary>
	public class IAPPurchasedEvent : GameServerEvent
	{
		public IAPPurchasedEvent(string playerId) : base(playerId)
		{
		}
		
	}
}

