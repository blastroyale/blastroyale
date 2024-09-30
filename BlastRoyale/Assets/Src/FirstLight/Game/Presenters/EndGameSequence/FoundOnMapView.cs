using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
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
		private (GameId, ushort)[] _found;
		[Q("RewardContainer")] private VisualElement _rewardContainer;

		protected override void Attached()
		{
			Element.SetDisplay(false);
		}

		public async UniTask Animate()
		{
			if (_found == null || _found.Length == 0)
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

		public void SetRewards(params (GameId, ushort)[] foundInMap)
		{
			_found = foundInMap;
			if (foundInMap == null || foundInMap.Length == 0)
			{
				FLog.Verbose("Nothing found on map");
				return;
			}

			// TODO: Replace by resource
			//var a = Resources.Load<VisualTreeAsset>("FoundRewardItem");
			var first = true;
			foreach (var reward in foundInMap)
			{
				var container = new VisualElement();
				container.AddToClassList("found-reward");
				if (first)
				{
					container.AddToClassList("found-reward--first");
					first = false;
				}

				var icon = new VisualElement();
				icon.AddToClassList("found-reward--item");
				container.Add(icon);
				var amt = new LabelOutlined(reward.Item2.ToString());
				amt.AddToClassList("found-reward--amount");
				container.Add(amt);
				var itemView = ItemFactory.Currency(reward.Item1, 0).GetViewModel();
				itemView.DrawIcon(icon);
				_rewardContainer.Add(container);
			}
		}
	}
}