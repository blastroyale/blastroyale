using System;
using System.Collections.Generic;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Loot Screen, where players can equip items and upgrade loot.
	/// </summary>
	public class LootScreenPresenter : UiToolkitPresenterData<LootScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<GameIdGroup> OnSlotButtonClicked;
			public Action OnBackButtonClicked;
		}

		private List<EquipmentSlotElement> _categories;
		private VisualElement _specialsHolder;
		private Label _mightLabel;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_specialsHolder = root.Q("SpecialsHolder").Required();
			_mightLabel = root.Q<Label>("MightLabel").Required();
			_categories = root.Query<EquipmentSlotElement>().Build().ToList();

			foreach (var cat in _categories)
			{
				cat.clicked += () => Data.OnSlotButtonClicked(cat.Category);
			}

			root.Q<ImageButton>("CloseButton").clicked += Data.OnBackButtonClicked;
			root.Q<ImageButton>("ScreenHeader").clicked += Data.OnBackButtonClicked;
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			RefreshCategories();
			RefreshSpecials();
			RefreshMight();
		}

		private void RefreshCategories()
		{
			foreach (var element in _categories)
			{
				if (_gameDataProvider.EquipmentDataProvider.Loadout.TryGetValue(element.Category, out var uniqueId))
				{
					var equipment = _gameDataProvider.EquipmentDataProvider.Inventory[uniqueId];
					element.SetEquipment(equipment);
				}
				else
				{
					element.SetEquipment(default);
				}
			}
		}

		private void RefreshSpecials()
		{
			_specialsHolder.Clear();

			if (_gameDataProvider.EquipmentDataProvider.Loadout.TryGetValue(GameIdGroup.Weapon, out var uniqueId))
			{
				var info = _gameDataProvider.EquipmentDataProvider.GetInfo(uniqueId);

				foreach (var (type, value) in info.Stats)
				{
					if (value > 0 && type is EquipmentStatType.SpecialId0 or EquipmentStatType.SpecialId1)
					{
						_specialsHolder.Add(new SpecialDisplayElement((GameId) value));
					}
				}
			}
		}

		private void RefreshMight()
		{
			var loadout = _gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.Both);
			var might = loadout.GetTotalMight(_services.ConfigsProvider.GetConfigsDictionary<QuantumStatConfig>());

			_mightLabel.text = string.Format(ScriptLocalization.UITEquipment.might, might.ToString("F0"));
		}
	}
}