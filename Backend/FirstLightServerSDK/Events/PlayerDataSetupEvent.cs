namespace FirstLight.Server.SDK.Models
{
	/// <summary>
	/// Event when a new player data is setup.
	/// This means a new player joined and we creating new data models for that player.
	/// Only happens once per player.
	/// </summary>
	public class PlayerDataSetupEvent : GameServerEvent
	{
		public PlayerDataSetupEvent(string player) : base(player)
		{
		}
	}
}