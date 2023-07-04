using System;
using System.Threading.Tasks;

namespace FirstLight.Server.SDK.Models
{
	public interface IDataSync
	{
		Task<bool> SyncData(string player);

		void Register();
	}

	public abstract class BaseEventDataSync<EventType> : IDataSync where EventType : GameServerEvent
	{
		private PluginContext _ctx;
		
		public BaseEventDataSync(PluginContext ctx)
		{
			_ctx = ctx;
		}
		
		public void Register()
		{
			_ctx.PluginEventManager.RegisterEventListener<EventType>(OnEvent);
		}

		private async Task OnEvent(EventType e)
		{
			await SyncData(e.PlayerId);
		}

		public abstract Task<bool> SyncData(string player);
	}
}