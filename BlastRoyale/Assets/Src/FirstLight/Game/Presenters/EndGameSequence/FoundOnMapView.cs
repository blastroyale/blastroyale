using System.Collections.Generic;
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

			var temp = new Dictionary<GameId, int>(_rewards.CollectedRewards);
			foreach (var (key, value) in _rewards.CollectedBonuses)
			{
				if (temp.TryGetValue(key, out var collected))
				{
					temp[key] = collected - value;
				}
			}

			var foundInMap = CreateItemList(temp, "Collected in Map");
			tooltip.Add(foundInMap);

			var nftBonus = CreateItemList(_rewards.CollectedBonuses, "NFT Buffs");
			tooltip.Add(nftBonus);

			_rewardContainer.OpenTooltip(Presenter.Root, tooltip);
		}

		private VisualElement CreateItemList(Dictionary<GameId, int> currencies, string title)
		{
			if (currencies.Count == 0)
			{
				FLog.Verbose("No buffs to display");
				return null;
			}

			var parent = new VisualElement().AddClass("item-list");
			parent.Add(new LabelOutlined(title).AddClass("item-list__title"));
			var holder = CreateItemList(currencies);
			parent.Add(holder);
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