using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Views.UITK
{
	public class MatchSettingsView : UIView
	{
		private IGameServices _services;

		private MatchSettingsButtonElement _modeButton;
		private MatchSettingsButtonElement _mapButton;
		private MatchSettingsButtonElement _teamSizeButton;
		private MatchSettingsButtonElement _maxPlayersButton;

		private ScrollView _mutatorsScroller;
		private ImageButton _mutatorsButton;

		private LocalizedButton _mainActionButton;

		public Action MainActionClicked { get; set; }
		public Action<CustomMatchSettings> MatchSettingsChanged { get; set; }
        
		public CustomMatchSettings MatchSettings { get; private set; }

		private int _selectedModeIndex;

		protected override void Attached()
		{
			_services = MainInstaller.ResolveServices();

			_modeButton = Element.Q<MatchSettingsButtonElement>("ModeButton").Required();
			_teamSizeButton = Element.Q<MatchSettingsButtonElement>("TeamSizeButton").Required();
			_mapButton = Element.Q<MatchSettingsButtonElement>("MapButton").Required();
			_maxPlayersButton = Element.Q<MatchSettingsButtonElement>("MaxPlayersButton").Required();

			_mutatorsScroller = Element.Q<ScrollView>("MutatorsScroller").Required();
			_mutatorsButton = Element.Q<ImageButton>("MutatorsButton").Required();

			_mainActionButton = Element.Q<LocalizedButton>("MainActionButton");
			_mainActionButton.clicked += () => MainActionClicked.Invoke();

			_modeButton.clicked += OnGameModeClicked;
			_teamSizeButton.clicked += OnTeamSizeClicked;
			_mapButton.clicked += OnMapClicked;
			_maxPlayersButton.clicked += OnMaxPlayersClicked;
			_mutatorsButton.clicked += OnMutatorsClicked;
		}

		public void SetMatchSettings(CustomMatchSettings settings, bool editable)
		{
			MatchSettings = settings;
			Element.SetEnabled(editable);
			RefreshData(false);
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
				MatchSettings.SquadSize = val;
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
			_mapButton.SetValue(MatchSettings.MapID);
			_maxPlayersButton.SetValue(MatchSettings.MaxPlayers.ToString());

			_mutatorsScroller.Clear();
			foreach (var mutator in MatchSettings.Mutators.GetSetFlags())
			{
				var mutatorLabel = new LocalizedLabel(mutator.GetLocalizationKey());
				mutatorLabel.AddToClassList("mutator");

				_mutatorsScroller.Add(mutatorLabel);
			}

			if (newSettings)
			{
				MatchSettingsChanged?.Invoke(MatchSettings);
			}
		}
	}
}