using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
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
	// TODO: REMOVE THIS!
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

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
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
			
			//_services.MessageBrokerService.Publish(new EquipmentScreenOpenedMessage());
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
	}
}