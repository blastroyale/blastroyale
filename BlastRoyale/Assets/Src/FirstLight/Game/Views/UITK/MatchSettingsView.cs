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
using QuickEye.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	public class MatchSettingsView : UIView
	{
		private IGameServices _services;

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

		[Q("PrivateRoomToggle")] private Toggle _privateRoomToggle;
		[Q("ShowCreatorNameToggle")] private Toggle _showCreatorNameToggle;

		[Q("MainActionButton")] private LocalizedButton _mainActionButton;

		public Action MainActionClicked { get; set; }
		public Action<CustomMatchSettings> MatchSettingsChanged { get; set; }

		public CustomMatchSettings MatchSettings { get; private set; }

		private int _selectedModeIndex;
		private bool _mutatorsTurnedOn;
		private bool _weaponFilterTurnedOn;

		protected override void Attached()
		{
			_services = MainInstaller.ResolveServices();

			_mainActionButton.clicked += () => MainActionClicked.Invoke();

			_modeButton.clicked += OnGameModeClicked;
			_teamSizeButton.clicked += OnTeamSizeClicked;
			_mapButton.clicked += OnMapClicked;
			_maxPlayersButton.clicked += OnMaxPlayersClicked;
			_mutatorsButton.clicked += OnMutatorsClicked;
			_filterWeaponsButton.clicked += OnWeaponFilterClicked;

			_mutatorsToggle.RegisterValueChangedCallback(OnMutatorsToggle);
			_filterWeaponsToggle.RegisterValueChangedCallback(OnWeaponFilterToggle);
			_privateRoomToggle.RegisterValueChangedCallback(v => MatchSettings.PrivateRoom = v.newValue);
			_showCreatorNameToggle.RegisterValueChangedCallback(v => MatchSettings.ShowCreatorName = v.newValue);
		}

		private void OnMutatorsToggle(ChangeEvent<bool> e)
		{
			_mutatorsTurnedOn = e.newValue;
			_mutatorsContainer.EnableInClassList("horizontal-scroll-picker--hidden", !e.newValue);

			if (!e.newValue)
			{
				MatchSettings.Mutators = Mutator.None;
				_mutatorsScroller.Clear();
			}
		}

		private void OnWeaponFilterToggle(ChangeEvent<bool> e)
		{
			_weaponFilterTurnedOn = e.newValue;
			_filterWeaponsContainer.EnableInClassList("horizontal-scroll-picker--hidden", !e.newValue);

			if (!e.newValue)
			{
				MatchSettings.WeaponFilter.Clear();
				_filterWeaponsScroller.Clear();
			}
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

		private void OnWeaponFilterClicked()
		{
			// TODO mihak: Implement weapon filter
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
			_privateRoomToggle.value = MatchSettings.PrivateRoom;
			_showCreatorNameToggle.value = MatchSettings.ShowCreatorName;

			_mutatorsScroller.Clear();
			var mutators = MatchSettings.Mutators.GetSetFlags();
			_mutatorsToggle.value = mutators.Length > 0 || _mutatorsTurnedOn;
			_mutatorsContainer.EnableInClassList("horizontal-scroll-picker--hidden",
				!_mutatorsToggle.value); // TODO mihak: I shouldn't have to do this

			foreach (var mutator in mutators)
			{
				var mutatorLabel = new LocalizedLabel(mutator.GetLocalizationKey());

				_mutatorsScroller.Add(mutatorLabel);
			}

			_filterWeaponsScroller.Clear();
			var weaponFilter = MatchSettings.WeaponFilter;
			_filterWeaponsToggle.value = weaponFilter.Count > 0 || _weaponFilterTurnedOn;
			_filterWeaponsContainer.EnableInClassList("horizontal-scroll-picker--hidden",
				!_filterWeaponsToggle.value); // TODO mihak: I shouldn't have to do this

			foreach (var weapon in weaponFilter)
			{
				_filterWeaponsScroller.Add(new LocalizedLabel(weapon)); // TODO mihak: Add localization key
			}

			if (weaponFilter.Count == 0)
			{
				_filterWeaponsScroller.Add(new LocalizedLabel("All"));
			}

			if (newSettings)
			{
				MatchSettingsChanged?.Invoke(MatchSettings);
			}
		}
	}
}