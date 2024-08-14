using System;
using System.Collections.Generic;
using System.Linq;
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
		[Q("RandomizeTeams")] private LocalizedToggle _randomizeTeamsToggle;
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
			_randomizeTeamsToggle.RegisterValueChangedCallback(OnRandomizeTeamsToggle);
			_privateRoomToggle.RegisterValueChangedCallback(v => MatchSettings.PrivateRoom = v.newValue);
			_showCreatorNameToggle.RegisterValueChangedCallback(v => MatchSettings.ShowCreatorName = v.newValue);
			_spectatorToggle.RegisterValueChangedCallback(v => SpectatorChanged(v.newValue).Forget());
			_botDifficultySlider.Q("unity-drag-container").RegisterCallback<PointerUpEvent, MatchSettingsView>((e, arg) =>
			{
				arg.MatchSettings.BotDifficulty = _botDifficultySlider.value;
				arg.RefreshData(true);
			}, this);
		}

		private void OnRandomizeTeamsToggle(ChangeEvent<bool> evt)
		{
			MatchSettings.RandomizeTeams = evt.newValue;
			if(evt.newValue != evt.previousValue) RefreshData(true);
		}

		private void OnAllowBotsToggle(ChangeEvent<bool> evt)
		{
			_botDifficultySlider.EnableInClassList(BOT_SLIDER_HIDDEN, !evt.newValue);
			_botDifficultySlider.SetValueWithoutNotify(MatchSettings.BotDifficulty = evt.newValue ? 5 : 0);

			if(evt.newValue != evt.previousValue) RefreshData(true);
		}

		private void OnMutatorsToggle(ChangeEvent<bool> evt)
		{
			_mutatorsTurnedOn = evt.newValue;
			_mutatorsContainer.EnableInClassList(HORIZONTAL_SCROLL_PICKER_HIDDEN, !evt.newValue);

			if (!evt.newValue)
			{
				MatchSettings.Mutators = Mutator.None;
				_mutatorsScroller.Clear();
			}

			if(evt.newValue != evt.previousValue) RefreshData(true);
		}

		private void OnWeaponFilterToggle(ChangeEvent<bool> evt)
		{
			_weaponFilterTurnedOn = evt.newValue;
			_filterWeaponsContainer.EnableInClassList(HORIZONTAL_SCROLL_PICKER_HIDDEN, !evt.newValue);

			if (!evt.newValue)
			{
				MatchSettings.WeaponFilter.Clear();
				_filterWeaponsScroller.Clear();
			}

			if(evt.newValue != evt.previousValue) RefreshData(true);
		}

		public void SetMatchSettings(CustomMatchSettings settings, bool editable, bool showSpectators)
		{
			MatchSettings = settings;

			SetEditable(editable);

			_bigTitle.SetVisibility(!showSpectators);
			_tabsContainer.SetVisibility(showSpectators);
			RefreshData(false);
		}

		private void SetEditable(bool editable)
		{
			_modeButton.SetEnabled(editable);
			_teamSizeButton.SetEnabled(editable);
			_mapButton.SetEnabled(editable);
			_maxPlayersButton.SetEnabled(editable && _services.FLLobbyService.CurrentMatchLobby == null);
			_mutatorsToggle.SetEnabled(editable);
			_mutatorsButton.SetDisplay(editable);
			_filterWeaponsToggle.SetEnabled(editable);
			_filterWeaponsButton.SetDisplay(editable);
			_allowBotsToggle.SetEnabled(editable);
			_botDifficultySlider.SetEnabled(editable);
			_randomizeTeamsToggle.SetEnabled(editable);
		}

		public void SetSpectators(List<Player> spectators)
		{
			_spectatorsScrollView.Clear();

			if (spectators.Any(s => s.IsLocal()))
			{
				_spectatorToggle.SetValueWithoutNotify(true);
				_spectatorToggle.SetEnabled(_services.FLLobbyService.CurrentMatchLobby.HasRoomInGrid());
			}
			foreach (var player in spectators)
			{
				var isHost = player.Id == _services.FLLobbyService.CurrentMatchLobby.HostId;
				var isLocal = player.Id == AuthenticationService.Instance.PlayerId;
				var playerElement = new MatchLobbyPlayerElement(player.GetPlayerName(), isHost, isLocal, false, false);

				_spectatorsScrollView.Add(playerElement);

				playerElement.clicked += () =>
				{
					var buttons = new List<PlayerContextButton>();
					if (_services.FLLobbyService.CurrentMatchLobby.IsLocalPlayerHost())
					{
						buttons.Add(new PlayerContextButton(PlayerButtonContextStyle.Red, ScriptLocalization.UITCustomGames.option_kick,
							() => _services.FLLobbyService.KickPlayerFromMatch(player.Id).Forget()));
					}
				
					_services.GameSocialService.OpenPlayerOptions(playerElement, Presenter.Root, player.Id, player.GetPlayerName(), new PlayerContextSettings()
					{
						ExtraButtons = buttons,
					});
				};
			
			}
		}

		private void OnMapClicked()
		{
			var options = _services.ConfigsProvider.GetConfigsList<QuantumGameModeConfig>()
				.Where(cfg => cfg.Id == MatchSettings.GameModeID)
				.SelectMany(cfg => cfg.AllowedMaps)
				.Distinct();

			PopupPresenter.OpenSelectMap(mapId =>
			{
				MatchSettings.MapID = mapId;
				RefreshData(true);
				PopupPresenter.Close().Forget();
			}, options, MatchSettings.MapID, false).Forget();
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
			_mainActionButton.LocalizationKey = labelKey;
			MainActionClicked = action;
		}

		private void RefreshData(bool newSettings)
		{
			_modeButton.SetValue(MatchSettings.GameModeID);
			_teamSizeButton.SetValue(MatchSettings.SquadSize.ToString());
			_mapButton.SetValue(Enum.Parse<GameId>(MatchSettings.MapID).GetLocalization());
			_maxPlayersButton.SetValue(MatchSettings.MaxPlayers.ToString());
			_privateRoomToggle.SetValueWithoutNotify(MatchSettings.PrivateRoom);
			_showCreatorNameToggle.SetValueWithoutNotify(MatchSettings.ShowCreatorName);
			_randomizeTeamsToggle.SetValueWithoutNotify(MatchSettings.RandomizeTeams);

			_mutatorsScroller.Clear();
			var mutators = MatchSettings.Mutators.GetSetFlags();
			_mutatorsToggle.SetValueWithoutNotify(mutators.Length > 0 || _mutatorsTurnedOn);
			_allowBotsToggle.SetValueWithoutNotify(MatchSettings.BotDifficulty > 0);
			_botDifficultySlider.SetValueWithoutNotify(MatchSettings.BotDifficulty);
			_botDifficultySlider.EnableInClassList(BOT_SLIDER_HIDDEN, !_allowBotsToggle.value);
			_mutatorsContainer.EnableInClassList(HORIZONTAL_SCROLL_PICKER_HIDDEN, !_mutatorsToggle.value);

			foreach (var mutator in mutators)
			{
				var mutatorLabel = new LocalizedLabel(mutator.GetLocalizationKey());

				_mutatorsScroller.Add(mutatorLabel);
			}

			_filterWeaponsScroller.Clear();
			var weaponFilter = MatchSettings.WeaponFilter;
			_filterWeaponsToggle.SetValueWithoutNotify(weaponFilter.Count > 0 || _weaponFilterTurnedOn);
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