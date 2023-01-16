using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Loot Screen, where players can equip items and upgrade loot.
	/// </summary>
	[LoadSynchronously]
	public class EquipmentPresenter : UiToolkitPresenterData<EquipmentPresenter.StateData>
	{
		public struct StateData
		{
			public Action<GameIdGroup> OnSlotButtonClicked;
			public Action OnHomeClicked;
			public Action OnBackClicked;
		}

		private List<EquipmentSlotElement> _categories;
		private VisualElement _specialsHolder;
		private MightElement _might;

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
			_might = root.Q<MightElement>("Might").Required();
			_categories = root.Query<EquipmentSlotElement>().Build().ToList();

			foreach (var cat in _categories)
			{
				cat.clicked += () => Data.OnSlotButtonClicked(cat.Category);
			}

			var header = root.Q<ScreenHeaderElement>("Header").Required();
			header.backClicked += Data.OnBackClicked;
			header.homeClicked += Data.OnHomeClicked;

			root.SetupClicks(_services);
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
				var unseenItems = _gameDataProvider.EquipmentDataProvider.Inventory
					.Any(pair => pair.Value.GameId.IsInGroup(element.Category) &&
						_gameDataProvider.UniqueIdDataProvider.NewIds.Contains(pair.Key));

				if (_gameDataProvider.EquipmentDataProvider.Loadout.TryGetValue(element.Category, out var uniqueId))
				{
					var info = _gameDataProvider.EquipmentDataProvider.GetInfo(uniqueId);

					element.SetEquipment(info, false, unseenItems);
				}
				else
				{
					element.SetEquipment(default, false, unseenItems);
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
						var specialId = (GameId) value;
						var element = new SpecialDisplayElement(specialId);
						element.clicked += () =>
							element.OpenTooltip(Root, specialId.GetDescriptionLocalization(), TooltipDirection.TopRight,
								TooltipPosition.BottomLeft, 20, 20);

						_specialsHolder.Add(element);
					}
				}
			}
		}

		private void RefreshMight()
		{
			var loadout = _gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.All);
			var might = loadout.GetTotalMight(_services.ConfigsProvider);

			_might.SetMight(might, false);
		}
	}
}