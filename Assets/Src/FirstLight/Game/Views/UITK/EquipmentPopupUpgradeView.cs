using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Infos;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles the upgrade content on the equipment popup
	/// </summary>
	public class EquipmentPopupUpgradeView : IUIView
	{
		private Label _currentLvl;
		private Label _nextLvl;
		private ListView _statsList;
		private PriceButton _upgradeButton;

		private Action _confirmAction;

		private List<Tuple<EquipmentStatType, float, float>> _statItems = new();


		public void Attached(VisualElement element)
		{
			_currentLvl = element.Q<Label>("LevelCurrent").Required();
			_nextLvl = element.Q<Label>("LevelNext").Required();
			_statsList = element.Q<ListView>("StatsList").Required();
			_upgradeButton = element.Q<PriceButton>("UpgradeButton").Required();

			_upgradeButton.clicked += () => _confirmAction();
		}

		public void SetData(EquipmentInfo info, Action confirmAction, bool insufficient)
		{
			_currentLvl.text = string.Format(ScriptLocalization.UITEquipment.popup_upgrade_lvl, info.Equipment.Level);
			_nextLvl.text = string.Format(ScriptLocalization.UITEquipment.popup_upgrade_lvl, info.Equipment.Level + 1);

			_upgradeButton.SetVisibility(!info.IsNft);
			_upgradeButton.SetPrice(info.UpgradeCost, insufficient);

			_confirmAction = confirmAction;

			_statsList.makeItem = () => new EquipmentStatBarElement();
			_statsList.bindItem = BindStatItem;

			// Stats
			_statItems.Clear();
			foreach (var pair in info.Stats)
			{
				if (!EquipmentStatBarElement.CanShowStat(pair.Key, pair.Value)) continue;

				var nextValue = info.NextLevelStats[pair.Key];
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

		public void SubscribeToEvents()
		{
		}

		public void UnsubscribeFromEvents()
		{
		}
	}
}