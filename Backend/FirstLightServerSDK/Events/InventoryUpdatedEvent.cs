namespace FirstLight.Server.SDK.Events
{
	/// <summary>
	/// Called when a player updates inventory in playfab
	/// </summary>
	public class InventoryUpdatedEvent : GameServerEvent
	{
		public InventoryUpdatedEvent(string playerId) : base(playerId)
		{
		}
		
	}
}

