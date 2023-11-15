using System;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;


namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Season change banner
	/// </summary>
	public class BattlePassSeasonBannerPresenter : UiToolkitPresenter
	{
        
		private Label _seasonText;
		private Label _timeLeft;
		private VisualElement[] _rewards;
		private VisualElement _finalReward;

		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);
			_seasonText = root.Q<Label>("SeasonText").Required();
			var rewards = root.Q("Rewards").Required();
			_timeLeft = root.Q<Label>("TimeLeft").Required();
			_rewards = rewards.Children().Select(r => r.Q("RewardIcon").Required()).ToArray();
			_finalReward = root.Q("FinalRewardIcon").Required();
			root.Q<Button>("StartButton").Required().clicked += OnClick;
			root.Q<Button>("CloseButton").clicked += ClosePopup;
			root.Q<VisualElement>("Blocker").RegisterCallback<ClickEvent>(ClickedOutside);
		}

		private void ClickedOutside(ClickEvent evt)
		{
			ClosePopup();
		}

		private void ClosePopup()
		{
			Close(true);
		}

		private void OnClick()
		{
			
			var s = MainInstaller.ResolveServices();
			s.GameUiService.CloseUi(this);
			s.MessageBrokerService.Publish(new NewBattlePassSeasonMessage());
		}
		
		protected override void OnOpened()
		{
			base.OnOpened();
			var data = MainInstaller.ResolveData();
			var currentSeason = data.BattlePassDataProvider.GetCurrentSeasonConfig();
			var endsAt = currentSeason.Season.GetEndsAtDateTime();
			
			_timeLeft.text = (endsAt - DateTime.UtcNow).ToDayAndHours(true);
			_seasonText.text = string.Format(ScriptLocalization.UITBattlePass.season_number, currentSeason.Season.Number);
			
			var lastGoodRewardIndex = 0;
			var rewards = data.BattlePassDataProvider.GetRewardConfigs(currentSeason.Levels.Select(l => (uint)l.RewardId), PassType.Paid);
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
			foreach (var goodie in goodies) rewards.Remove(goodie);
			while (goodies.Count < 3)
			{
				var reward = rewards.First();
				goodies.Add(reward);
				rewards.Remove(reward);
			}

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
		}

		private bool ShouldShowcase(EquipmentRewardConfig reward)
		{
			return reward.GameId.IsInGroup(GameIdGroup.Collection);
		}
	}
}
