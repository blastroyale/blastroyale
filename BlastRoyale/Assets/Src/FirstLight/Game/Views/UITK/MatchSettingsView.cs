using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using QuickEye.UIToolkit;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class MatchSettingsView : UIView
	{
		private IGameServices _services;

		private const string BOT_SLIDER_HIDDEN = "bots-slider--hidden";
		private const string HORIZONTAL_SCROLL_PICKER_HIDDEN = "horizontal-scroll-picker--hidden";

		[Q("unity-tabs-container")] private VisualElement _tabsContainer;
		[Q("Title")] private LocalizedLabel _bigTitle;
		[Q("GameInfo")] private VisualElement _gameInfoContainer;

		[Q("ModeButton")] private MatchSettingsButtonElement _modeButton;
		[Q("MapButton")] private MatchSettingsButtonElement _mapButton;
		[Q("TeamSizeButton")] private MatchSettingsButtonElement _teamSizeButton;
		[Q("MaxPlayersButton")] private MatchSettingsButtonElement _maxPlayersButton;

		[Q("Mutators")] private VisualElement _mutatorsContainer;
		[Q("MutatorsToggle")] private LocalizedToggle _mutatorsToggle;
		[Q("MutatorsScroller")] private ScrollView _mutatorsScroller;
		[Q("MutatorsButton")] private ImageButton _mutatorsButton;
		[Q("FilterWeapons")] private VisualElement _filterWeaponsContainer;
		[Q("FilterWeaponsToggle")] private LocalizedToggle _filterWeaponsToggle;
		[Q("FilterWeaponsScroller")] private ScrollView _filterWeaponsScroller;
		[Q("FilterWeaponsButton")] private ImageButton _filterWeaponsButton;
		[Q("AllowBotsToggle")] private LocalizedToggle _allowBotsToggle;
		[Q("BotDifficultySlider")] private SliderInt _botDifficultySlider;

		[Q("PrivateRoomToggle")] private Toggle _privateRoomToggle;
		[Q("ShowCreatorNameToggle")] private Toggle _showCreatorNameToggle;

		[Q("SpectatorToggle")] private LocalizedToggle _spectatorToggle;
		[Q("SpectatorsScrollView")] private ScrollView _spectatorsScrollView;

		[Q("MainActionButton")] private LocalizedButton _mainActionButton;

		public Action MainActionClicked { get; set; }
		public Action<CustomMatchSettings> MatchSettingsChanged { get; set; }
		public Func<bool, UniTaskVoid> SpectatorChanged { get; set; }

		public CustomMatchSettings MatchSettings { get; private set; }

		private int _selectedModeIndex;
		private bool _mutatorsTurnedOn;
		private bool _weaponFilterTurnedOn;

		protected override void Attached()
		{
			_services = MainInstaller.ResolveServices();

			_gameInfoContainer = Element.Q("GameInfo").Q("unity-content-container").Required();

			_mainActionButton.clicked += () => MainActionClicked.Invoke();
			_modeButton.clicked += OnGameModeClicked;
			_teamSizeButton.clicked += OnTeamSizeClicked;
			_mapButton.clicked += OnMapClicked;
			_maxPlayersButton.clicked += OnMaxPlayersClicked;
			_mutatorsButton.clicked += OnMutatorsClicked;
			_filterWeaponsButton.clicked += OnWeaponFilterClicked;

			_mutatorsToggle.RegisterValueChangedCallback(OnMutatorsToggle);
			_filterWeaponsToggle.RegisterValueChangedCallback(OnWeaponFilterToggle);
			_allowBotsToggle.RegisterValueChangedCallback(OnAllowBotsToggle);
			_privateRoomToggle.RegisterValueChangedCallback(v => MatchSettings.PrivateRoom = v.newValue);
			_showCreatorNameToggle.RegisterValueChangedCallback(v => MatchSettings.ShowCreatorName = v.newValue);
			_spectatorToggle.RegisterValueChangedCallback(v => SpectatorChanged(v.newValue).Forget());
			_botDifficultySlider.Q("unity-drag-container").RegisterCallback<ClickEvent, MatchSettingsView>((e, arg) =>
			{
				arg.MatchSettings.BotDifficulty = _botDifficultySlider.value;
				arg.RefreshData(true);
			}, this);
		}

		private void OnAllowBotsToggle(ChangeEvent<bool> e)
		{
			_botDifficultySlider.EnableInClassList(BOT_SLIDER_HIDDEN, !e.newValue);
			_botDifficultySlider.value = MatchSettings.BotDifficulty = e.newValue ? 1 : 0;

			RefreshData(true);
		}

		private void OnMutatorsToggle(ChangeEvent<bool> e)
		{
			_mutatorsTurnedOn = e.newValue;
			_mutatorsContainer.EnableInClassList(HORIZONTAL_SCROLL_PICKER_HIDDEN, !e.newValue);

			if (!e.newValue)
			{
				MatchSettings.Mutators = Mutator.None;
				_mutatorsScroller.Clear();
			}

			RefreshData(true);
		}

		private void OnWeaponFilterToggle(ChangeEvent<bool> e)
		{
			_weaponFilterTurnedOn = e.newValue;
			_filterWeaponsContainer.EnableInClassList(HORIZONTAL_SCROLL_PICKER_HIDDEN, !e.newValue);

			if (!e.newValue)
			{
				MatchSettings.WeaponFilter.Clear();
				_filterWeaponsScroller.Clear();
			}

			RefreshData(true);
		}

		public void SetMatchSettings(CustomMatchSettings settings, bool editable, bool showSpectators)
		{
			MatchSettings = settings;
			
			// If we make the container not enabled scroll view will not work
			foreach (var visualElement in _gameInfoContainer.Children())
			{
				visualElement.SetEnabled(editable);
			}

			_bigTitle.SetVisibility(!showSpectators);
			_tabsContainer.SetVisibility(showSpectators);
			RefreshData(false);
		}

		public void SetSpectators(List<Player> spectators)
		{
			_spectatorsScrollView.Clear();

			foreach (var player in spectators)
			{
				var isHost = player.Id == _services.FLLobbyService.CurrentMatchLobby.HostId;
				var isLocal = player.Id == AuthenticationService.Instance.PlayerId;
				var playerElement = new MatchLobbyPlayerElement(player.GetPlayerName(), isHost, isLocal, false, false);

				_spectatorsScrollView.Add(playerElement);

				// TODO: Implement tooltip
			}
		}

		private void OnMapClicked()
		{
			PopupPresenter.OpenSelectMap(mapId =>
			{
				MatchSettings.MapID = mapId;
				RefreshData(true);
				PopupPresenter.Close().Forget();
			}, MatchSettings.GameModeID, MatchSettings.MapID).Forget();
		}

		private void OnMaxPlayersClicked()
		{
			// TODO mihak: These numbers should be somewhere else
			PopupPresenter.OpenSelectNumber(val =>
			{
				MatchSettings.MaxPlayers = val;
				RefreshData(true);
				PopupPresenter.Close().Forget();
			}, ScriptTerms.UITCustomGames.max_players, ScriptTerms.UITCustomGames.max_players_desc, 2, 48, MatchSettings.MaxPlayers).Forget();
		}

		private void OnTeamSizeClicked()
		{
			PopupPresenter.OpenSelectSquadSize(val =>
			{
				MatchSettings.SquadSize = (uint) val;
				RefreshData(true);
				PopupPresenter.Close().Forget();
			}, MatchSettings.SquadSize).Forget();
		}

		private void OnMutatorsClicked()
		{
			PopupPresenter.OpenSelectMutators(mutators =>
			{
				MatchSettings.Mutators = mutators;
				RefreshData(true);
				PopupPresenter.Close().Forget();
			}, MatchSettings.Mutators).Forget();
		}

		private void OnWeaponFilterClicked()
		{
			PopupPresenter.OpenSelectWeapons(weapons =>
			{
				MatchSettings.WeaponFilter = weapons;
				RefreshData(true);
				PopupPresenter.Close().Forget();
			}, MatchSettings.WeaponFilter).Forget();
		}

		private void OnGameModeClicked()
		{
			// We currently only support changing the game mode in debug builds
			if (!Debug.isDebugBuild) return;

			var gameModes = _services.ConfigsProvider.GetConfigsList<QuantumGameModeConfig>();
			_selectedModeIndex = (_selectedModeIndex + 1) % gameModes.Count;
			var selectedGameMode = gameModes[_selectedModeIndex];
			MatchSettings.GameModeID = selectedGameMode.Id;

			RefreshData(true);
		}

		public void SetMainAction(string labelKey, Action action)
		{
			if (labelKey == null || action == null)
			{
				_mainActionButton.SetDisplay(false);
				return;
			}

			_mainActionButton.SetDisplay(true);
			_mainActionButton.Localize(labelKey);
			MainActionClicked = action;
		}

		private void RefreshData(bool newSettings)
		{
			_modeButton.SetValue(MatchSettings.GameModeID);
			_teamSizeButton.SetValue(MatchSettings.SquadSize.ToString());
			_mapButton.SetValue(Enum.Parse<GameId>(MatchSettings.MapID).GetLocalization());
			_maxPlayersButton.SetValue(MatchSettings.MaxPlayers.ToString());
			_privateRoomToggle.value = MatchSettings.PrivateRoom;
			_showCreatorNameToggle.value = MatchSettings.ShowCreatorName;

			_mutatorsScroller.Clear();
			var mutators = MatchSettings.Mutators.GetSetFlags();
			_mutatorsToggle.value = mutators.Length > 0 || _mutatorsTurnedOn;
			_allowBotsToggle.value = MatchSettings.BotDifficulty > 0;
			_botDifficultySlider.value = MatchSettings.BotDifficulty;
			_botDifficultySlider.EnableInClassList(BOT_SLIDER_HIDDEN, !_allowBotsToggle.value);
			_mutatorsContainer.EnableInClassList(HORIZONTAL_SCROLL_PICKER_HIDDEN, !_mutatorsToggle.value);

			foreach (var mutator in mutators)
			{
				var mutatorLabel = new LocalizedLabel(mutator.GetLocalizationKey());

				_mutatorsScroller.Add(mutatorLabel);
			}

			_filterWeaponsScroller.Clear();
			var weaponFilter = MatchSettings.WeaponFilter;
			_filterWeaponsToggle.value = weaponFilter.Count > 0 || _weaponFilterTurnedOn;
			_filterWeaponsContainer.EnableInClassList(HORIZONTAL_SCROLL_PICKER_HIDDEN, !_filterWeaponsToggle.value);

			foreach (var weapon in weaponFilter)
			{
				_filterWeaponsScroller.Add(new LocalizedLabel(Enum.Parse<GameId>(weapon).GetLocalizationKey()));
			}

			if (weaponFilter.Count == 0)
			{
				_filterWeaponsScroller.Add(new LocalizedLabel(ScriptTerms.UITCustomGames.all));
			}

			if (newSettings)
			{
				MatchSettingsChanged?.Invoke(MatchSettings);
			}
		}
	}
}