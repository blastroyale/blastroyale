using System.Linq;
using BuffSystem;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
using QuickEye.UIToolkit;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	public class BuffsView : UIView
	{
		private BuffVirtualEntity _buffs;
		private MatchRewardsResult _result;

		[Q("BuffsHolder")] private VisualElement _holder;

		protected override void Attached()
		{
			Element.SetDisplay(false);
		}

		public async UniTask Animate()
		{
			if (_buffs == null || _buffs.Stats.Count == 0)
			{
				return;
			}

			var duration = 150;
			Element.style.opacity = 0;
			Element.SetDisplay(true);
			Element.AnimatePing();
			Element.AnimateOpacity(0, 1, duration);
			await UniTask.Delay(duration);
		}

		public void SetBuffs(BuffVirtualEntity e, MatchRewardsResult allRewards)
		{
			_buffs = e;
			_result = allRewards;
			if (allRewards.Bonuses.Count == 0)
			{
				FLog.Verbose("No buffs to display");
				return;
			}

			_holder.Clear();
			Element.RegisterCallback<ClickEvent>(OnClicked);
			foreach (var (id, value) in allRewards.Bonuses)
			{
				var buff = new VisualElement().AddClass("nft-buff");
				var label = new LabelOutlined(value.ToString()).AddClass("nft-buff__label");
				var icon = CurrencyItemViewModel.CreateIcon(id).AddClass("nft-buff__icon");
				buff.Add(icon);
				buff.Add(label);
				_holder.Add(buff);
			}
		}

		private void OnClicked(ClickEvent evt)
		{
			var service = MainInstaller.ResolveServices().BuffService;
			var elements = _buffs.Stats.Where(kv => kv.Key != BuffStat.PctBonusBurnForRewards)
				.Select((kv) =>
				{
					var holder = new VisualElement();
					holder.AddClass("buff-tooltip__holder");
					var label = new LabelOutlined("+" + (kv.Value.FinalValue.AsInt) + "%");
					label.AddClass("buff-tooltip__label");
					holder.Add(label);
					var icon = CurrencyItemViewModel.CreateIcon(service.GetRelatedGameId(kv.Key));
					icon.AddClass("buff-tooltip__icon");
					holder.Add(icon);
					return holder;
				});

			var tooltip = new VisualElement();
			tooltip.AddClass("buff-tooltip");
			foreach (var visualElement in elements)
			{
				tooltip.Add(visualElement);
			}

			_holder.OpenTooltip(Presenter.Root, tooltip);
		}
	}
}