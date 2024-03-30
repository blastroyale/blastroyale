using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Modules.UIService.Runtime;
using FirstLight.UiService;
using FirstLight.UIService;
using Quantum;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Season change banner
	/// </summary>
	[UILayer(UIService2.UILayer.Popup)]
	public class BattlePassSeasonBannerPresenter : UIPresenter2
	{
		private IGameServices _services;

		private Label _timeLeft;
		private VisualElement[] _rewards;
		private VisualElement _finalReward;
		private Cooldown _closeCooldown;


		private void Awake()
		{
			_services = MainInstaller.ResolveServices();
		}

		protected override void QueryElements()
		{
			var rewards = Root.Q("Rewards").Required();
			_timeLeft = Root.Q<Label>("TimeLeft").Required();
			_rewards = rewards.Children().Select(r => r.Q("RewardIcon").Required()).ToArray();
			_finalReward = Root.Q("FinalRewardIcon").Required();
			Root.Q<Button>("StartButton").Required().clicked += OnClick;
			Root.Q<Button>("CloseButton").clicked += ClosePopup;
			Root.Q<VisualElement>("Blocker").RegisterCallback<PointerDownEvent>(ClickedOutside);
			_closeCooldown = new Cooldown(TimeSpan.FromSeconds(2));
		}

		private void ClickedOutside(PointerDownEvent evt)
		{
			if (_closeCooldown.IsCooldown()) return;
			ClosePopup();
		}

		private void ClosePopup()
		{
			_services.UIService.CloseScreen(this).Forget();
		}

		private void OnClick()
		{
			var s = MainInstaller.ResolveServices();
			ClosePopup();
			s.MessageBrokerService.Publish(new NewBattlePassSeasonMessage());
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_closeCooldown.Trigger();
			var data = MainInstaller.ResolveData();
			var currentSeason = data.BattlePassDataProvider.GetCurrentSeasonConfig();
			var endsAt = currentSeason.Season.GetEndsAtDateTime();

			_timeLeft.text = (endsAt - DateTime.UtcNow).ToDayAndHours(true);


			var goodies = GetHighlightedManual();
			if (goodies.Count < 4)
			{
				goodies = GetHighlightedAutomatic();
			}

			var best = goodies.Last();
			goodies.Remove(best);
			var lastRewardView = ItemFactory.Legacy(new LegacyItemData()
			{
				RewardId = best.GameId,
				Value = best.Amount
			}).GetViewModel();
			lastRewardView.DrawIcon(_finalReward);

			for (var x = 0; x < 3; x++)
			{
				var item = ItemFactory.Legacy(new LegacyItemData()
				{
					RewardId = goodies[x].GameId,
					Value = goodies[x].Amount
				});
				var itemView = item.GetViewModel();
				itemView.DrawIcon(_rewards[x]);
			}

			return base.OnScreenOpen(reload);
		}

		private List<EquipmentRewardConfig> GetHighlightedManual()
		{
			var data = MainInstaller.ResolveData();
			var currentSeason = data.BattlePassDataProvider.GetCurrentSeasonConfig();

			var highlighted = currentSeason.Season.GetHighlighted();
			var goodies = highlighted.Select(t => data.BattlePassDataProvider.GetRewardConfigs(new[] {t.level}, t.passType).First()).ToList();

			return goodies;
		}

		private List<EquipmentRewardConfig> GetHighlightedAutomatic()
		{
			var data = MainInstaller.ResolveData();
			var currentSeason = data.BattlePassDataProvider.GetCurrentSeasonConfig();
			var type = currentSeason.Season.RemovePaid ? PassType.Free : PassType.Paid;

			var rewards = data.BattlePassDataProvider.GetRewardConfigs(currentSeason.Levels.Select((a, e) => (uint) e + 1), type);
			rewards.Reverse();

			var bestReward = rewards.First();
			rewards.Remove(bestReward);
			var lastRewardView = ItemFactory.Legacy(new LegacyItemData()
			{
				RewardId = bestReward.GameId,
				Value = bestReward.Amount
			}).GetViewModel();
			lastRewardView.DrawIcon(_finalReward);

			var goodies = rewards.Where(ShouldShowcase).ToList();
			goodies.Add(bestReward);
			foreach (var goodie in goodies) rewards.Remove(goodie);
			while (goodies.Count < 3)
			{
				var reward = rewards.First();
				goodies.Add(reward);
				rewards.Remove(reward);
			}

			return goodies;
		}

		private bool ShouldShowcase(EquipmentRewardConfig reward)
		{
			return reward.GameId.IsInGroup(GameIdGroup.Collection);
		}
	}
}