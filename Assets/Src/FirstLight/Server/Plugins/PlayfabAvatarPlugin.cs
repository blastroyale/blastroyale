using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Data.DataTypes.Helpers;
using FirstLight.Game.Logic;
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
			_ctx.PluginEventManager.RegisterEventListener<PlayerDataSetupEvent>(OnPlayerSetup);
			_ctx.PluginEventManager.RegisterEventListener<GameLogicMessageEvent<CollectionItemEquippedMessage>>(OnGameLogicMessageEvent);
		}

		private async Task OnPlayerSetup(PlayerDataSetupEvent e)
		{
			var defaultItem = DefaultCollectionItems.Items[CollectionCategories.PROFILE_PICTURE].First();
			await SetupPlayerAvatarUrl(e.PlayerId, defaultItem);
		}
		
		private async Task OnGameLogicMessageEvent(GameLogicMessageEvent<CollectionItemEquippedMessage> ev)
		{
			if (ev.Message.Category == CollectionCategories.PROFILE_PICTURE)
			{
				await SetupPlayerAvatarUrl(ev.PlayerId, ev.Message.EquippedItem);
			}
		}

		private async Task SetupPlayerAvatarUrl(string playerId, ItemData avatar)
		{
			var config = _ctx.GameConfig.GetConfig<AvatarCollectableConfig>();
			var url = AvatarHelpers.GetAvatarUrl(avatar, config);
			await _ctx.PlayerProfile.UpdatePlayerAvatarURL(playerId, url);
		}
	}
}