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
	/// Handles the upgrade content on the equipment popup
	/// </summary>
	public class EquipmentPopupUpgradeView : IUIView
	{
		private Label _currentLvl;
		private Label _nextLvl;
		private ListView _statsList;
		private LocalizedButton _upgradeButton;

		private Action _confirmAction;

		public void Attached(VisualElement element)
		{
			_currentLvl = element.Q<Label>("LevelCurrent").Required();
			_nextLvl = element.Q<Label>("LevelNext").Required();
			_statsList = element.Q<ListView>("StatsList").Required();
			_upgradeButton = element.Q<LocalizedButton>("UpgradeButton").Required();

			_upgradeButton.clicked += () => _confirmAction();
		}

		public void SetData(EquipmentInfo info, Action confirmAction)
		{
			_currentLvl.text = string.Format(ScriptLocalization.UITEquipment.popup_upgrade_lvl, info.Equipment.Level);
			_nextLvl.text = string.Format(ScriptLocalization.UITEquipment.popup_upgrade_lvl, info.Equipment.Level + 1);

			_upgradeButton.SetVisibility(!info.IsNft);

			_confirmAction = confirmAction;

			_statsList.makeItem = () => new EquipmentStatBarElement();
			_statsList.bindItem = BindStatItem;

			// TODO: Set stats from Miguel's data
			_statsList.itemsSource = new List<string> {"", "", ""};
		}

		private void BindStatItem(VisualElement element, int index)
		{
			var statElement = (EquipmentStatBarElement) element;

			// TODO: Set stats from Miguel's data
			statElement.SetUpgradeValue(EquipmentStatType.Hp, 500, 800);
		}

		public void SubscribeToEvents()
		{
		}

		public void UnsubscribeFromEvents()
		{
		}
	}
}