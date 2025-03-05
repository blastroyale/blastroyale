using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Server.SDK.Models
{
	/// <summary>
	/// Always executed before a command runs
	/// </summary>
	public class BeforeCommandRunsEvent : GameServerEvent
	{
		public string PlayerId;
		public IGameCommand CommandInstance;
		public ServerState State;
		public bool CancelCommand;
		
		public BeforeCommandRunsEvent(string player) : base(player)
		{
		}
	}
}