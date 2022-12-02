using FirstLight.Game.Messages;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using Newtonsoft.Json;


namespace Src.FirstLight.Server
{
	/// <summary>
	/// Client implementation of which events should be fired on analytics server-side from game logic
	/// execution.
	/// </summary>
	public class ServerAnalyticsPlugin : ServerPlugin
	{
		private PluginContext _ctx;

		public override void OnEnable(PluginContext context)
		{
			_ctx = context;
			var evManager = _ctx.PluginEventManager!;
			evManager.RegisterListener<GameLogicMessageEvent<PlayerSkinUpdatedMessage>>(OnSkinUpdate);
			evManager.RegisterListener<GameLogicMessageEvent<GameCompletedRewardsMessage>>(OnGameCompleted);
			evManager.RegisterListener<GameLogicMessageEvent<BattlePassLevelUpMessage>>(OnBattlePassRewards);
			evManager.RegisterListener<GameLogicMessageEvent<ItemScrappedMessage>>(OnItemScrapped);
			evManager.RegisterListener<GameLogicMessageEvent<ItemUpgradedMessage>>(OnItemUpgraded);
		}

		private void OnItemUpgraded(GameLogicMessageEvent<ItemUpgradedMessage> ev)
		{
			var data = new AnalyticsData
			{
				{"item_uid", ev.Message.Id},
				{"item_id", ev.Message.GameId},
				{"item_name", ev.Message.Name},
				{"durability", ev.Message.Durability},
				{"level", ev.Message.Level},
				{"coin_spending", ev.Message.Price.Value},
			};

			_ctx.Analytics!.EmitUserEvent(ev.UserId, "item_upgrade", data);
		}

		private void OnItemScrapped(GameLogicMessageEvent<ItemScrappedMessage> ev)
		{
			var data = new AnalyticsData
			{
				{"item_uid", ev.Message.Id},
				{"item_id", ev.Message.GameId},
				{"item_name", ev.Message.Name},
				{"durability", ev.Message.Durability},
				{"coin_earning", ev.Message.Reward.Value},
			};

			_ctx.Analytics!.EmitUserEvent(ev.UserId, "item_scrap", data);
		}

		private void OnGameCompleted(GameLogicMessageEvent<GameCompletedRewardsMessage> ev)
		{
			var data = new AnalyticsData
			{
				{"trophies_change", ev.Message.TrophiesChange},
				{"trophies_before_change", ev.Message.TrophiesBeforeChange},
			};
			if (ev.Message.Rewards != null)
			{
				data["rewards"] = JsonConvert.SerializeObject(ev.Message.Rewards);
			}

			_ctx.Analytics!.EmitUserEvent(ev.UserId, "game_completed_rewards", data);
		}

		private void OnBattlePassRewards(GameLogicMessageEvent<BattlePassLevelUpMessage> ev)
		{
			var data = new AnalyticsData
			{
				{"new_level", ev.Message.newLevel},
			};
			if (ev.Message.Rewards != null)
			{
				data["rewards"] = JsonConvert.SerializeObject(ev.Message.Rewards);
			}

			_ctx.Analytics!.EmitUserEvent(ev.UserId, "battle_pass_rewards", data);
		}

		private void OnSkinUpdate(GameLogicMessageEvent<PlayerSkinUpdatedMessage> ev)
		{
			_ctx.Analytics!.EmitUserEvent(ev.UserId, "character_changed", new AnalyticsData()
			{
				{"new_skin", ev.Message.SkinId.ToString()}
			});
		}
	}
}