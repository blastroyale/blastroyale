using System;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
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

		private Button _mainActionButton;

		public Action MainActionClicked { get; set; }
		public CustomMatchSettings MatchSettings { get; private set; }

		// TODO: Remove when we have popups
		private int _selectedModeIndex;
		private int _selectedMapIndex;

		protected override void Attached()
		{
			_services = MainInstaller.ResolveServices();

			_modeButton = Element.Q<MatchSettingsButtonElement>("ModeButton").Required();
			_teamSizeButton = Element.Q<MatchSettingsButtonElement>("TeamSizeButton").Required();
			_mapButton = Element.Q<MatchSettingsButtonElement>("MapButton").Required();
			_maxPlayersButton = Element.Q<MatchSettingsButtonElement>("MaxPlayersButton").Required();

			_mutatorsScroller = Element.Q<ScrollView>("MutatorsScroller").Required();
			_mutatorsButton = Element.Q<ImageButton>("MutatorsButton").Required();

			_mainActionButton = Element.Q<Button>("MainActionButton");
			_mainActionButton.clicked += () => MainActionClicked.Invoke();

			_modeButton.Button.clicked += OnGameModeClicked;
			_teamSizeButton.Button.clicked += OnTeamSizeClicked;
			_mapButton.Button.clicked += OnMapClicked;
		}

		public void SetMatchSettings(CustomMatchSettings settings, bool editable)
		{
			MatchSettings = settings;
			Element.SetEnabled(editable);
			RefreshData();
		}

		private void OnMapClicked()
		{
			// TODO: Replace with popup
			var maps = _services.ConfigsProvider.GetConfigsList<QuantumMapConfig>();

			_selectedMapIndex = (_selectedMapIndex + 1) % maps.Count;
			var selectedMap = maps[_selectedMapIndex];

			MatchSettings.MapID = selectedMap.Map.ToString();

			RefreshData();
		}

		private void OnTeamSizeClicked()
		{
			MatchSettings.TeamSize = (MatchSettings.TeamSize + 1) % 4 + 1; // TODO mihak: 4 should be somewhere else
			RefreshData();
		}

		private void OnGameModeClicked()
		{
			// TODO: Replace with popup
			var gameModes = _services.ConfigsProvider.GetConfigsList<QuantumGameModeConfig>();

			_selectedModeIndex = (_selectedModeIndex + 1) % gameModes.Count;
			var selectedGameMode = gameModes[_selectedModeIndex];

			MatchSettings.GameModeID = selectedGameMode.Id;

			RefreshData();
		}

		public void SetMainAction(string label, Action action)
		{
			if (label == null || action == null)
			{
				_mainActionButton.SetDisplay(false);
				return;
			}

			_mainActionButton.SetDisplay(true);
			_mainActionButton.text = label;
			MainActionClicked = action;
		}

		private void RefreshData()
		{
			_modeButton.SetValue(MatchSettings.GameModeID);
			_teamSizeButton.SetValue(MatchSettings.TeamSize.ToString());
			_mapButton.SetValue(MatchSettings.MapID);
			_maxPlayersButton.SetValue(MatchSettings.MaxPlayers.ToString());

			_mutatorsScroller.Clear();
			foreach (var mutator in MatchSettings.Mutators)
			{
				var mutatorLabel = new Label(mutator);
				mutatorLabel.AddToClassList("mutator");

				_mutatorsScroller.Add(mutatorLabel);
			}
		}
	}
}