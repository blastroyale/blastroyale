using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using Newtonsoft.Json;
using Quantum;

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
			evManager.RegisterEventListener<GameLogicMessageEvent<GameCompletedRewardsMessage>>(OnGameCompleted);
			evManager.RegisterEventListener<GameLogicMessageEvent<BattlePassLevelUpMessage>>(OnBattlePassRewards);
			evManager.RegisterEventListener<GameLogicMessageEvent<CurrencyChangedMessage>>(OnCurrencyChanged);
			evManager.RegisterEventListener<GameLogicMessageEvent<PurchaseClaimedMessage>>(OnPurchasedItemRewarded);
			evManager.RegisterCommandListener<EndOfGameCalculationsCommand>(OnGameEndCommand);
		}

		private Task OnGameEndCommand(string userId, EndOfGameCalculationsCommand cmd, ServerState state)
		{
			var simulationConfig = SimulationMatchConfig.FromByteArray(cmd.SerializedSimulationConfig);
			var player = cmd.PlayersMatchData[cmd.QuantumValues.ExecutingPlayer];
			var data = new AnalyticsData()
			{
				{"match_id", cmd.QuantumValues.MatchId},
				{"match_type", simulationConfig.MatchType.ToString()},
				{"game_mode", player.GameModeId},
				{"map_id", player.MapId},
				{"players_left", cmd.PlayersMatchData.Count(d => !d.IsBot).ToString()},
				{"suicide", player.Data.SuicideCount.ToString()},
				{"kills", player.Data.PlayersKilledCount.ToString()},
				{"player_rank", player.PlayerRank.ToString()},
				{"damage_done", player.Data.DamageDone.ToString() },
				{"damage_received", player.Data.DamageReceived.ToString() },
				{"death_count", player.Data.DeathCount.ToString() },
				{"healing_done", player.Data.HealingDone.ToString() },
				{"healing_received", player.Data.HealingReceived.ToString() },
				{"initial_trophies", player.Data.PlayerTrophies.ToString() },
				{"final_trophies",  state.DeserializeModel<PlayerData>().Trophies.ToString() },
				{"first_death_time", player.Data.FirstDeathTime.AsLong.ToString() },
				{"last_death_position", player.Data.LastDeathPosition.ToString() },
				{"specials_used", player.Data.SpecialsUsedCount.ToString() },
				{"team_size", simulationConfig.TeamSize.ToString() },
				{"team_id", player.Data.TeamId.ToString() },
			};
			_ctx.Analytics!.EmitUserEvent(userId, $"server_match_end_summary", data);
			return Task.CompletedTask;
		}

		private Task OnCurrencyChanged(GameLogicMessageEvent<CurrencyChangedMessage> ev)
		{
			var data = new AnalyticsData(new Dictionary<string, object>()
			{
				{"amount", System.Math.Abs(ev.Message.Change)},
				{"category", ev.Message.Category},
				{"balance", ev.Message.NewValue}
			});

			// Event name is coin_earning, coin_spending, cs_earning etc...
			var eventName = ev.Message.Id.ToString().ToLowerInvariant() + "_" +
				(ev.Message.Change > 0 ? "earning" : "spending");
			_ctx.Analytics!.EmitUserEvent(ev.PlayerId, eventName, data);
			return Task.CompletedTask;
		}
		
		private Task OnPurchasedItemRewarded(GameLogicMessageEvent<PurchaseClaimedMessage> ev)
		{
			var data = new AnalyticsData
			{
				{"item_name", Enum.GetName(typeof(GameId), ev.Message.ItemPurchased.Id)},
				{"item_metadata", JsonConvert.SerializeObject(ev.Message.ItemPurchased)},
			};

			if (!string.IsNullOrEmpty(ev.Message.SupportingContentCreator))
			{
				data["content_creator_code"] = ev.Message.SupportingContentCreator;
			}

			if (!string.IsNullOrEmpty(ev.Message.PriceCurrencyId))
			{
				data["price_purchased_id"] = ev.Message.PriceCurrencyId;
				data["price_paid_value"] = ev.Message.PricePaid;
			}

			_ctx.Analytics!.EmitUserEvent(ev.PlayerId, "purchased_item", data);
			return Task.CompletedTask;
		}

		private Task OnGameCompleted(GameLogicMessageEvent<GameCompletedRewardsMessage> ev)
		{
			var data = new AnalyticsData
			{
				{"trophies_change_value", ev.Message.TrophiesChange.ToString(CultureInfo.InvariantCulture)},
				{"trophies_before_change_value", ev.Message.TrophiesBeforeChange.ToString(CultureInfo.InvariantCulture)},
			};
			if (ev.Message.Rewards.FinalRewards != null)
			{
				data["rewards"] = ModelSerializer.Serialize(ev.Message.Rewards.FinalRewards).Value;
			}
			if (ev.Message.Rewards.CollectedRewards != null)
			{
				data["collected"] = ModelSerializer.Serialize(ev.Message.Rewards.CollectedRewards).Value;
			}
			if (ev.Message.Rewards.CollectedBonuses != null)
			{
				data["collectedbuffs"] = ModelSerializer.Serialize(ev.Message.Rewards.CollectedBonuses).Value;
			}
			if (ev.Message.Rewards.BonusFromWinning != null)
			{
				data["winbonus"] = ModelSerializer.Serialize(ev.Message.Rewards.BonusFromWinning).Value;
			}

			_ctx.Analytics!.EmitUserEvent(ev.PlayerId, "game_completed_rewards", data);
			return Task.CompletedTask;
		}

		private Task OnBattlePassRewards(GameLogicMessageEvent<BattlePassLevelUpMessage> ev)
		{
			var data = new AnalyticsData
			{
				{"new_pass_level", ev.Message.NewLevel.ToString(CultureInfo.InvariantCulture)},
			};
			if (ev.Message.Rewards != null)
			{
				data["rewards"] = JsonConvert.SerializeObject(ev.Message.Rewards.Select(e => e.Id.ToString()));
			}

			_ctx.Analytics!.EmitUserEvent(ev.PlayerId, "battle_pass_rewards", data);
			return Task.CompletedTask;
		}
	}
}