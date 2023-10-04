using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;


namespace Src.FirstLight.Server
{
	/// <summary>
	/// Plugin implementation ...
	/// </summary>
	public class PlayfabAvatarPlugin : ServerPlugin
	{
		private PluginContext _ctx;

		public override void OnEnable(PluginContext context)
		{
			_ctx = context;
			_ctx.PluginEventManager.RegisterEventListener<GameLogicMessageEvent<CollectionItemEquippedMessage>>(OnGameLogicMessageEvent);
		}
		
		private async Task OnGameLogicMessageEvent(GameLogicMessageEvent<CollectionItemEquippedMessage> ev)
		{
			if (ev.Message.Category == CollectionCategories.PROFILE_PICTURE)
			{
				var config = _ctx.GameConfig.GetConfig<AvatarCollectableConfig>();
				var url = config.GameIdUrlDictionary[ev.Message.EquippedItem.Id];
				
				await _ctx.PlayerProfile.UpdatePlayerAvatarURL(ev.PlayerId, url);
			}
		}
	}
}