using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;


namespace FirstLight.Game.Presenters
{
	public class FoundOnMapView : UIView
	{
		private (GameId, ushort)[] _found;
		private List<VisualElement> _foundWidgets;
		
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
			foreach (var r in _foundWidgets)
			{
				r.AnimatePing();
				await UniTask.Delay(200);
			}
		}
		
		public void SetRewards(params (GameId, ushort) [] foundInMap)
		{
			_found = foundInMap;
			if (foundInMap == null || foundInMap.Length == 0)
			{
				FLog.Verbose("Nothing found on map");
				return;
			}

			_foundWidgets = new List<VisualElement>();
			// TODO: Replace by resource
			//var a = Resources.Load<VisualTreeAsset>("FoundRewardItem");
			foreach (var reward in foundInMap)
			{
				var container = new VisualElement();
				container.AddToClassList("found-reward");
				var icon = new VisualElement();
				icon.AddToClassList("found-reward--item");
				container.Add(icon);
				var amt = new LabelOutlined(reward.Item2.ToString());
				amt.AddToClassList("found-reward--amount");
				container.Add(amt);
				Element.Add(container);
				var itemView = ItemFactory.Currency(reward.Item1, 1).GetViewModel();
				itemView.DrawIcon(icon);
				_foundWidgets.Add(container);
			}		
		}
	}
}