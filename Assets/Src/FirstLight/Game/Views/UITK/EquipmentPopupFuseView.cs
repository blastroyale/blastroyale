using System;
using System.Collections.Generic;
using FirstLight.Game.Infos;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles the fusion content on the equipment popup
	/// </summary>
	public class EquipmentPopupFuseView : UIView
	{
		private const string UssPriceInsufficient = "requirements--insufficient";
		private const string UssSpriteCurrency = "sprite-shared__icon-currency-{0}";
		private const string UssCurrencyIconStyle = "requirements_icon";

		private Label _currentRarity;
		private Label _nextRarity;
		private ListView _statsList;
		private PriceButton _fuseButton;
		private VisualElement _bottomFiller;

		private Action _confirmAction;

		private readonly List<Tuple<EquipmentStatType, float, float>> _statItems = new();

		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			_currentRarity = element.Q<Label>("RarityCurrent").Required();
			_nextRarity = element.Q<Label>("RarityNext").Required();
			_statsList = element.Q<ListView>("StatsList").Required();
			_fuseButton = element.Q<PriceButton>("FusePriceButton").Required();
			_bottomFiller = element.Q<VisualElement>("BottomFiller").Required();

			_statsList.DisableScrollbars();

			_fuseButton.clicked += () => _confirmAction();
		}

		public void SetData(EquipmentInfo info, Action confirmAction, bool[] insufficient)
		{
			var canPurchase = insufficient[1];
			_currentRarity.text = string.Format(info.Equipment.Rarity.ToString());
			_nextRarity.text = string.Format((info.Equipment.Rarity + 1).ToString());

			_fuseButton.SetEnabled(!canPurchase);
			_fuseButton.SetPrice(info.FuseCost[0], info.IsNft, insufficient[1], canPurchase);

			_confirmAction = confirmAction;

			_statsList.makeItem = () => new EquipmentStatBarElement();
			_statsList.bindItem = BindStatItem;

			// Stats
			_statItems.Clear();
			foreach (var pair in info.Stats)
			{
				if (!EquipmentStatBarElement.CanShowStat(pair.Key, pair.Value)) continue;

				var nextValue = info.NextRarityStats[pair.Key];
				if (pair.Value < nextValue)
				{
					_statItems.Add(new Tuple<EquipmentStatType, float, float>(pair.Key, pair.Value, nextValue));
				}
			}

			_statItems.Sort((x1, x2) => x1.Item1.CompareTo(x2.Item1));
			_statsList.itemsSource = _statItems;
			_statsList.RefreshItems();
		}

		private void BindStatItem(VisualElement element, int index)
		{
			var statElement = (EquipmentStatBarElement) element;
			var stat = _statItems[index];

			statElement.SetValue(stat.Item1, stat.Item2, true, stat.Item3);
		}
	}
}