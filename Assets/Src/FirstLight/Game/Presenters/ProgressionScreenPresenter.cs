using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.GridViews;
using FirstLight.Game.Views.MainMenuViews;
using TMPro;
using UnityEngine;
using FirstLight.Game.Infos;
using I2.Loc;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This View handles the Equipment / Loot Menu.
	/// </summary>
	public class ProgressionScreenPresenter : AnimatedUiPresenterData<ProgressionScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnProgressMenuClosedClicked;
		}

		[SerializeField] private TextMeshProUGUI _noAdventuresText;
		[SerializeField] private Button _eventButton;
		[SerializeField] private GameObject _eventSelection;
		[SerializeField] private DifficultyProgressToggleView[] _toggles;
		
		[Header("OSA")]
		[SerializeField] private Button _closeButton;
		[SerializeField] private GenericGridView _gridView;


		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private AdventureDifficultyLevel _filterSelection;
		private AllAdventuresInfo _allInfo;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_filterSelection = AdventureDifficultyLevel.Normal;
			
			foreach (var toggle in _toggles)
			{
				toggle.OnClickEvent.AddListener(OnFilterClicked);
			}
			
			_eventSelection.SetActive(false);
			_eventButton.onClick.AddListener(OnEventButtonClicked);
			_closeButton.onClick.AddListener(OnBackButtonPressed);
			_services.MessageBrokerService.Subscribe<AdventureFirstTimeRewardsCollectedMessage>(OnAdventureFirstTimeRewardsCollectedMessage);
			
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}
		
		protected override async void OnOpened()
		{
			base.OnOpened();

			_allInfo = _gameDataProvider.AdventureDataProvider.GetAllAdventuresInfo();
			_noAdventuresText.enabled = false;
			
			foreach (var toggle in _toggles)
			{
				toggle.SetInfo(_filterSelection, _allInfo);
			}
			
			_gridView.Clear();
			_eventSelection.SetActive(false);

			// This small delay is just to improve the flow of the ProgressMenuGridItemView animations
			await Task.Delay(500);
			
			_gridView.gameObject.SetActive(true);
			UpdateMenu();
			ScrollToCurrentAdventure();
		}
		
		private void OnFilterClicked(AdventureDifficultyLevel progressionFilter)
		{
			_filterSelection = progressionFilter;
			
			foreach (var toggle in _toggles)
			{
				toggle.SetInfo(_filterSelection, _allInfo);
			}
			
			_eventSelection.SetActive(false);
			_gridView.gameObject.SetActive(true);
			UpdateMenu();
			ScrollToCurrentAdventure();
		}

		private void UpdateMenu()
		{
			var dataProvider = _gameDataProvider.AdventureDataProvider;
			var list = new List<ProgressMenuGridItemView.ProgressMenuGridItemData>(_allInfo.Adventures.Count);

			foreach (var adventure in _allInfo.Adventures)
			{
				if (adventure.Config.Difficulty != _filterSelection)
				{
					continue;
				}
				
				var playCompletedAnimation = !dataProvider.AdventuresCompletedTagged.Contains(adventure.AdventureData.Id);
				var viewData = new ProgressMenuGridItemView.ProgressMenuGridItemData
				{
					Info = adventure, 
					PlayIntroAnimation = true,
					PlayAdventureCompletedAnimation = adventure.IsCompleted && playCompletedAnimation
				};

				list.Add(viewData);
			}

			_noAdventuresText.enabled = list.Count == 0;
			_noAdventuresText.text = string.Format(ScriptLocalization.MainMenu.NoAdventuresAvailable,
					_filterSelection.ToString().ToUpper());
			
			_gridView.UpdateData(list);
		}

		private void OnEventButtonClicked()
		{
			_noAdventuresText.enabled = true;
			_noAdventuresText.text = ScriptLocalization.MainMenu.EventsComingSoon;
			
			foreach (var toggle in _toggles)
			{
				toggle.SetInfo(AdventureDifficultyLevel.TOTAL, _allInfo);
			}
			
			_eventSelection.SetActive(true);
			_gridView.Clear();
			_gridView.gameObject.SetActive(false);
		}
		
		private void OnBackButtonPressed()
		{
			Data.OnProgressMenuClosedClicked();
		}

		private void ScrollToCurrentAdventure()
		{
			var index = 0;

			foreach (var adventure in _allInfo.Adventures)
			{
				if (adventure.Config.Difficulty != _filterSelection)
				{
					continue;
				}

				if (!adventure.AdventureData.RewardCollected && adventure.IsCompleted || 
				    !adventure.IsCompleted && adventure.IsUnlocked)
				{
						_gridView.ScrollTo(Mathf.Max(0, index - 1));
						break;
				}

				index++;
			}
		}
		
		private void OnAdventureFirstTimeRewardsCollectedMessage(AdventureFirstTimeRewardsCollectedMessage message)
		{
			_allInfo = _gameDataProvider.AdventureDataProvider.GetAllAdventuresInfo();
			UpdateMenu();
		}
	}
}