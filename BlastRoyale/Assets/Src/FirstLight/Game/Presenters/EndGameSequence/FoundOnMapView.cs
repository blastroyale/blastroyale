using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
using QuickEye.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;


namespace FirstLight.Game.Presenters
{
	public class FoundOnMapView : UIView
	{
		private MatchRewardsResult _rewards;
		private VisualElement _rewardContainer;

		protected override void Attached()
		{
			Element.SetDisplay(false);
			Element.RegisterCallback<ClickEvent>(OnClicked);
		}

		private void OnClicked(ClickEvent evt)
		{
			var tooltip = new VisualElement();
			
			var collectedInMap = new Dictionary<GameId, int>(_rewards.CollectedRewards);
			var winBonus = new Dictionary<GameId, int>(_rewards.BonusFromWinning);
			
			foreach (var (key, value) in _rewards.CollectedBonuses)
			{
				// Deduct non-collected to only show what was being collected
				if (collectedInMap.TryGetValue(key, out var collected))
				{
					collectedInMap[key] = collected - value;
					
					// Deduct win bonuses too so we show them separated
					if (winBonus.TryGetValue(key, out var wonAsBonus))
					{
						collectedInMap[key] -= wonAsBonus;
					}
				}
			}

			var rewardSections = new List<RewardSection>();
			if (collectedInMap?.Count > 0 && collectedInMap.Any(c => c.Value > 0))
			{
				rewardSections.Add(new RewardSection()
				{
					Title =  "Collected in Map",
					Currencies = collectedInMap
				});
			}

			if (winBonus?.Count > 0 && winBonus.Any(c => c.Value > 0))
			{
				rewardSections.Add(new RewardSection()
				{
					Title =  "Winner Bonus",
					Currencies = winBonus
				});
			}

			if (rewardSections.Count > 0)
			{
				var foundInMap = CreateItemList(rewardSections.ToArray());
				tooltip.Add(foundInMap);
			}

			var hasAnyNftBonus = _rewards.CollectedBonuses.Any(kv => kv.Value > 0);
			if (hasAnyNftBonus)
			{
				var nftBonus = CreateItemList(new RewardSection()
				{
					Title = "NFT Buffs",
					Currencies = _rewards.CollectedBonuses,
				});
				tooltip.Add(nftBonus);
			}
			_rewardContainer.OpenTooltip(Presenter.Root, tooltip);
		}

		public class RewardSection
		{
			public string Title;
			public Dictionary<GameId, int> Currencies;
		}
		
		private VisualElement CreateItemList(params RewardSection [] rewards)
		{
			
			if (rewards.Length == 0 || rewards.All(r => r.Currencies == null || r.Currencies.Count == 0))
			{
				return null;
			}

			var parent = new VisualElement().AddClass("item-list");
			foreach (var reward in rewards)
			{
				parent.Add(new LabelOutlined(reward.Title).AddClass("item-list__title"));
				var holder = CreateItemList(reward.Currencies);
				parent.Add(holder);
			}
			return parent;
		}

		private static VisualElement CreateItemList(Dictionary<GameId, int> currencies, bool large = false)
		{
			var holder = new VisualElement().AddClass("item-list__container");
			if (large)
			{
				holder.AddClass("item-list__container--large");
			}

			var first = true;
			foreach (var (id, value) in currencies)
			{
				var buff = new VisualElement().AddClass("item-list__item");
				if (first)

				{
					buff.AddClass("item-list__item--first");
					first = false;
				}

				var label = new LabelOutlined(value.ToString()).AddClass("item-list__item__label");
				var icon = CurrencyItemViewModel.CreateIcon(id).AddClass("item-list__item__icon");
				buff.Add(icon);
				buff.Add(label);
				holder.Add(buff);
			}

			return holder;
		}

		public async UniTask Animate()
		{
			if (_rewards == null)
			{
				return;
			}

			var duration = 150;
			Element.style.opacity = 0;
			Element.SetDisplay(true);
			Element.AnimateOpacity(0, 1, duration);
			foreach (var r in _rewardContainer.Children())
			{
				r.AnimatePing();
				await UniTask.Delay(200, cancellationToken: Presenter.GetCancellationTokenOnClose());
			}
		}

		public void SetRewards(MatchRewardsResult foundInMap)
		{
			_rewards = foundInMap;
			if (foundInMap == null || _rewards.CollectedRewards.Count == 0)
			{
				FLog.Verbose("Nothing found on map");
				return;
			}

			_rewardContainer = CreateItemList(_rewards.CollectedRewards, true);
			Element.Add(_rewardContainer);
		}
	}
}