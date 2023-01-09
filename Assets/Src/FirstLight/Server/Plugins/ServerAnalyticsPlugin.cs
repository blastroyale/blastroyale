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
			evManager.RegisterEventListener<GameLogicMessageEvent<PlayerSkinUpdatedMessage>>(OnSkinUpdate);
			evManager.RegisterEventListener<GameLogicMessageEvent<GameCompletedRewardsMessage>>(OnGameCompleted);
			evManager.RegisterEventListener<GameLogicMessageEvent<BattlePassLevelUpMessage>>(OnBattlePassRewards);
			evManager.RegisterEventListener<GameLogicMessageEvent<ItemScrappedMessage>>(OnItemScrapped);
			evManager.RegisterEventListener<GameLogicMessageEvent<ItemUpgradedMessage>>(OnItemUpgraded);
			evManager.RegisterEventListener<GameLogicMessageEvent<ItemRepairedMessage>>(OnItemRepaired);
			evManager.RegisterEventListener<GameLogicMessageEvent<CurrencyChangedMessage>>(OnCurrencyChanged);
		}

		private void OnCurrencyChanged(GameLogicMessageEvent<CurrencyChangedMessage> ev)
		{
			var data = new AnalyticsData
			{
				{"amount", System.Math.Abs(ev.Message.Change)},
				{"category", ev.Message.Category},
				{"balance", ev.Message.NewValue}
			};

			// Event name is coin_earning, coin_spending, cs_earning etc...
			var eventName = ev.Message.Id.ToString().ToLowerInvariant() + "_" +
				(ev.Message.Change > 0 ? "earning" : "spending");
			_ctx.Analytics!.EmitUserEvent(ev.UserId, eventName, data);
		}

		private void OnItemRepaired(GameLogicMessageEvent<ItemRepairedMessage> ev)
		{
			var data = new AnalyticsData
			{
				{"item_uid", ev.Message.Id},
				{"item_id", ev.Message.GameId},
				{"item_name", ev.Message.Name},
				{"durability", ev.Message.Durability},
				{"durability_final", ev.Message.DurabilityFinal},
				{"coin_spending", ev.Message.Price.Value}
			};

			_ctx.Analytics!.EmitUserEvent(ev.UserId, "item_repair", data);
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
				{"coin_spending", ev.Message.Price.Value}
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
				{"coin_earning", ev.Message.Reward.Value}
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