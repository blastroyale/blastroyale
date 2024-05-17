using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Party;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter is responsible to select the game mode to start the match
	/// </summary>
	public class GameModeScreenPresenter : UIPresenterData<GameModeScreenPresenter.StateData>
	{
		private const string VISIBLE_GAMEMODE_BUTTON = "visible-gamemodebutton";

		public class StateData
		{
			public Action<GameModeInfo> GameModeChosen;
			public Action CustomGameChosen;

			public Action OnHomeClicked;
			public Action OnBackClicked;
		}

		[SerializeField] private VisualTreeAsset _buttonAsset;
		[SerializeField] private VisualTreeAsset _comingSoonAsset;

		private Button _closeButton;
		private ScrollView _buttonsSlider;
		private ScreenHeaderElement _header;
		private LocalizedDropDown _mapDropDown;
		private List<GameId> _mapGameIds;

		private List<GameModeSelectionButtonView> _buttonViews;
		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements()
		{
			_buttonViews = new List<GameModeSelectionButtonView>();
			_buttonsSlider = Root.Q<ScrollView>("ButtonsSlider").Required();
			_header = Root.Q<ScreenHeaderElement>("Header").Required();
			_header.backClicked += Data.OnBackClicked;
			_mapDropDown = Root.Q<LocalizedDropDown>("Map").Required();
			FillMapSelectionList();

			var orderNumber = 1;

			// Add game modes buttons
			foreach (var slot in _services.GameModeService.Slots)
			{
				var button = _buttonAsset.Instantiate();
				button.userData = slot;
				button.AttachView(this, out GameModeSelectionButtonView view);
				view.SetData("GameModeButton" + orderNumber, GetVisibleClass(orderNumber++), slot);
				view.Clicked += OnModeButtonClicked;
				_buttonViews.Add(view);

				view.Disabled = slot.Entry.TeamSize < _services.PartyService.GetCurrentGroupSize();
				view.Selected = _services.GameModeService.SelectedGameMode.Value.Equals(slot);

				_buttonsSlider.Add(button);
			}

			// Add custom game button
			var gameModeInfo = new GameModeInfo();
			gameModeInfo.Entry.GameModeId = GameConstants.GameModeId.FAKEGAMEMODE_CUSTOMGAME;
			gameModeInfo.Entry.MatchType = MatchType.Custom;
			gameModeInfo.Entry.TitleTranslationKey = ScriptTerms.UITGameModeSelection.custom_game_title;
			gameModeInfo.Entry.DescriptionTranslationKey = ScriptTerms.UITGameModeSelection.custom_game_description;
			gameModeInfo.Entry.Mutators = new List<string>();
			var createGameButton = _buttonAsset.Instantiate();
			createGameButton.AttachView(this, out GameModeSelectionButtonView customGameView);
			customGameView.SetData("CustomGameButton", GetVisibleClass(orderNumber++), gameModeInfo);
			customGameView.Clicked += OnCustomGameClicked;
			customGameView.Disabled = _services.PartyService.HasParty.Value;
			_buttonViews.Add(customGameView);
			_buttonsSlider.Add(createGameButton);
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_services.GameModeService.Slots.Observe(OnSlotUpdated);
			_services.GameModeService.SelectedGameMode.Observe(OnGameModeUpdated);
			_services.PartyService.Members.Observe(OnPartyMembersChanged);
			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
			_services.GameModeService.Slots.StopObserving(OnSlotUpdated);
			_services.GameModeService.SelectedGameMode.StopObserving(OnGameModeUpdated);
			_services.PartyService.Members.StopObserving(OnPartyMembersChanged);
			return base.OnScreenClose();
		}

		private string GetVisibleClass(int orderNumber)
		{
			return VISIBLE_GAMEMODE_BUTTON + (orderNumber > 4 ? "" : orderNumber);
		}

		private void OnCustomGameClicked(GameModeSelectionButtonView info)
		{
			Data.CustomGameChosen();
		}

		private void OnSlotUpdated(int index, GameModeInfo previous, GameModeInfo current,
								   ObservableUpdateType updateType)
		{
			_buttonViews[index].SetData(current);
		}

		private void OnPartyMembersChanged(int index, PartyMember before, PartyMember after, ObservableUpdateType type)
		{
			if (type != ObservableUpdateType.Added && type != ObservableUpdateType.Removed)
			{
				return;
			}

			foreach (var view in _buttonViews)
			{
				if (view.GameModeInfo.Entry.MatchType == MatchType.Custom) continue;
				if (view.GameModeInfo.Entry.PlayfabQueue == null) continue;
				view.Disabled = view.GameModeInfo.Entry.PlayfabQueue.TeamSize < _services.PartyService.GetCurrentGroupSize();
			}
		}

		private void OnModeButtonClicked(GameModeSelectionButtonView info)
		{
			SelectButton(info);
			StartCoroutine(ChangeGameModeCoroutine(info));
		}

		private IEnumerator ChangeGameModeCoroutine(GameModeSelectionButtonView info)
		{
			_services.GameModeService.SelectedGameMode.Value = info.GameModeInfo;
			_services.GameModeService.SelectedMap = _mapGameIds[_mapDropDown.index]; 
			
			Data.GameModeChosen(info.GameModeInfo);
			yield return null;
		}

		private void SelectButton(GameModeSelectionButtonView info)
		{
			foreach (var buttonView in _buttonViews)
			{
				buttonView.Selected = false;
			}

			info.Selected = true;
		}

		/// <summary>
		/// Listen for selected gamemode changes, when party size changes it selects a proper gamemode matching the team size
		/// </summary>
		private void OnGameModeUpdated(GameModeInfo _, GameModeInfo newGamemode)
		{
			foreach (var buttonView in _buttonViews)
			{
				buttonView.Selected = buttonView.GameModeInfo.Entry == newGamemode.Entry;
			}
		}
		
		private void FillMapSelectionList()
		{
			var gameModeConfigs = _services.ConfigsProvider.GetConfigsList<QuantumGameModeConfig>();
			var menuChoices = new List<string>();
			_mapGameIds = new List<GameId>();

			foreach (var gameModeConfig in gameModeConfigs)
			{
				if (gameModeConfig.Id == "BattleRoyale")
				{
					foreach (var mapId in gameModeConfig.AllowedMaps)
					{
						var mapConfig = _services.ConfigsProvider.GetConfig<QuantumMapConfig>((int) mapId);
						if (!mapConfig.IsTestMap && !mapConfig.IsCustomOnly)
						{
							menuChoices.Add(mapId.GetLocalization());
							_mapGameIds.Add(mapId);
						}
					}
					
					if (_services.GameModeService.SelectedMap == 0)
					{
						_mapDropDown.index = 1;
					}
					
					_mapDropDown.choices = menuChoices;
					
					for (var i = 0; i < _mapGameIds.Count; i++)
					{
						if (_mapGameIds[i] == _services.GameModeService.SelectedMap)
						{
							_mapDropDown.index = i;
							break;
						}
					}
				}
			}
		}
	}
}