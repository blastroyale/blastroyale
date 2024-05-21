using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services.Party;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services
{
	public struct GameModeInfo
	{
		public GameModeRotationConfig.GameModeEntry Entry;
		public DateTime EndTime;

		public bool IsFixed => EndTime == default || EndTime.Ticks == 0;

		public GameModeInfo(GameModeRotationConfig.GameModeEntry entry, DateTime endTime = default)
		{
			Entry = entry;
			EndTime = endTime;
		}

		public override string ToString()
		{
			return $"Entry({Entry}), EndTime({EndTime}), IsFixed({IsFixed})";
		}
	}

	/// <summary>
	/// Stores and provides the currently selected GameMode / MapID to play and provides
	/// rotational (time limited) game modes.
	/// </summary>
	public interface IGameModeService
	{
		/// <summary>
		/// Sets up the initial game mode rotation values - must be called after configs are loaded.
		/// </summary>
		void Init();

		/// <summary>
		/// The currently selected GameMode.
		/// </summary>
		IObservableField<GameModeInfo> SelectedGameMode { get; }

		/// <summary>
		/// The currently selected Map.
		/// </summary>
		GameId SelectedMap { set; get; }

		/// <summary>
		/// Allowed maps to be chosen on matchmaking
		/// </summary>
		List<GameId> ValidMatchmakingMaps { get; }

		/// <summary>
		/// Provides a list of currently available game modes which is automatically updated when
		/// rotating game modes change.
		/// </summary>
		IObservableListReader<GameModeInfo> Slots { get; }

		/// <summary>
		/// Checks if a given game-mode is a valid entry for the rotation.
		/// Dates could be different hence not using `GameModeInfo` object as the same game mode
		/// in different "seasons" might apply.
		/// </summary>
		bool IsInRotation(GameModeRotationConfig.GameModeEntry gameMode);

		/// <summary>
		/// Gets the current map in the given game mode rotation
		/// </summary>
		QuantumMapConfig GetRotationMapConfig(string gameModeId);
	}

	/// <inheritdoc cref="IGameModeService"/>
	public class GameModeService : IGameModeService
	{
		private const string SelectedQueueLobbyProperty = "selected_queue";

		private readonly IConfigsProvider _configsProvider;
		private readonly IThreadService _threadService;
		private readonly IPartyService _partyService;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IAppDataProvider _appDataProvider;
		private readonly LocalPrefsService _localPrefsService;

		private readonly IObservableList<GameModeInfo> _slots;
		private GameId _selectedMap;

		public IObservableField<GameModeInfo> SelectedGameMode { get; }

		public GameId SelectedMap
		{
			set => _localPrefsService.SelectedRankedMap.Value = (int)value;
			get
			{
				if (_localPrefsService.SelectedRankedMap.Value == 0)
				{
					return GameId.MazeMayhem;
				}

				return (GameId) _localPrefsService.SelectedRankedMap.Value;
			}
		}

		public List<GameId> ValidMatchmakingMaps
		{
			get
			{
				var gameModeConfigs = _configsProvider.GetConfigsList<QuantumGameModeConfig>();
				var validMaps = new List<GameId>();
				foreach (var gameModeConfig in gameModeConfigs.Where(gameModeConfig => gameModeConfig.Id == "BattleRoyale"))
				{
					foreach (var mapId in gameModeConfig.AllowedMaps)
					{
						var mapConfig = _configsProvider.GetConfig<QuantumMapConfig>((int) mapId);
						if (!mapConfig.IsTestMap && !mapConfig.IsCustomOnly)
						{
							validMaps.Add(mapId);
						}
					}
				}

				return validMaps;
			}
		}

		public IObservableListReader<GameModeInfo> Slots => _slots;

		public GameModeService(IConfigsProvider configsProvider, IThreadService threadService,
							   IGameDataProvider gameDataProvider, IPartyService partyService,
							   IAppDataProvider appDataProvider,LocalPrefsService localPrefsService)
		{
			_configsProvider = configsProvider;
			_threadService = threadService;
			_gameDataProvider = gameDataProvider;
			_partyService = partyService;
			_appDataProvider = appDataProvider;
			_localPrefsService = localPrefsService;

			_slots = new ObservableList<GameModeInfo>(new List<GameModeInfo>());
			SelectedGameMode = new ObservableField<GameModeInfo>();
			SelectedGameMode.Observe(OnGameModeSet);
			_partyService.Members.Observe(OnPartyMemberUpdate);
			_partyService.HasParty.Observe((_, hasParty) => { OnHasPartyUpdate(hasParty); });
			_partyService.OnLobbyPropertiesCreated += AddGameModeToPartyProperties;
			_partyService.LobbyProperties.Observe(SelectedQueueLobbyProperty, OnLeaderChangedGameMode);
		}

		public void Init()
		{
			var config = _configsProvider.GetConfig<GameModeRotationConfig>();

			// Initially add empty objects which get updated by RefreshGameModes
			for (var i = 0; i < config.Slots.Count; i++)
			{
				_slots.Add(default);
			}

			RefreshGameModes(true);

			// Try to set the saved game mode
			var lastGameMode = _appDataProvider.LastGameMode;
			if (CanSelectGameMode(lastGameMode))
			{
				FLog.Verbose($"Restored selected game mode to: {lastGameMode}");
				SelectedGameMode.Value = new GameModeInfo(lastGameMode);
			}
			else
			{
				this.SelectDefaultRankedMode();
			}
		}

		private void OnGameModeSet(GameModeInfo _, GameModeInfo current)
		{
			FLog.Info($"Selected GameMode set to: {current}");

			_appDataProvider.LastGameMode = current.Entry;
			if (_appDataProvider.IsPlayerLoggedIn)
			{
				MainInstaller.Resolve<IGameServices>().DataSaver.SaveData<AppData>();
			}

			if (_partyService.HasParty.Value && _partyService.GetLocalMember().Leader)
			{
				var newQueueName = current.Entry.PlayfabQueue.QueueName;
				if (_partyService.LobbyProperties.TryGetValue(SelectedQueueLobbyProperty, out var currentPartyQueue))
				{
					if (currentPartyQueue == newQueueName)
					{
						return;
					}
				}
				_partyService.SetLobbyProperty(SelectedQueueLobbyProperty, newQueueName, true).Forget();
			}
		}

		private void AddGameModeToPartyProperties(Dictionary<string, string> _, Dictionary<string, string> lobbyData)
		{
			lobbyData[SelectedQueueLobbyProperty] = SelectedGameMode.Value.Entry.PlayfabQueue.QueueName;
		}

		private void OnLeaderChangedGameMode(string key, string previous, string current, ObservableUpdateType type)
		{
			var selected = SelectedGameMode.Value.Entry.PlayfabQueue.QueueName;
			if (selected == current)
			{
				return;
			}

			var newValue = _slots.FirstOrDefault(a => a.Entry.PlayfabQueue.QueueName == current);
			if (newValue.Entry.PlayfabQueue?.QueueName == null) return;
			SelectedGameMode.Value = newValue;
		}

		/// <summary>
		/// Returns the current map in rotation, used for creating rooms with maps in rotation
		/// </summary>
		public QuantumMapConfig GetRotationMapConfig(string gameModeId)
		{
			var services = MainInstaller.ResolveServices();
			var gameModeConfig = services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId);
			var compatibleMaps = new List<QuantumMapConfig>();
			var span = DateTime.UtcNow - DateTime.UtcNow.Date;
			var timeSegmentIndex =
				Mathf.RoundToInt((float) span.TotalMinutes / GameConstants.Balance.MAP_ROTATION_TIME_MINUTES);

			var mapConfigs = services.ConfigsProvider.GetConfigsDictionary<QuantumMapConfig>();

			foreach (var mapId in gameModeConfig.AllowedMaps)
			{
				if (!mapConfigs.TryGetValue((int) mapId, out var mapConfig))
				{
					FLog.Error($"Could not find map config for map {mapId} - maybe outdated AppData ?");
					continue;
				}

				if (!mapConfig.IsTestMap && !mapConfig.IsCustomOnly)
				{
					compatibleMaps.Add(mapConfig);
				}
			}

			if (timeSegmentIndex >= compatibleMaps.Count)
			{
				timeSegmentIndex %= compatibleMaps.Count;
			}

			return compatibleMaps[timeSegmentIndex];
		}

		private void OnPartyMemberUpdate(int index, PartyMember before, PartyMember after, ObservableUpdateType type)
		{
			if (type == ObservableUpdateType.Added || type == ObservableUpdateType.Removed)
			{
				AutoSelectGameModeForTeamSize(_partyService.Members.Count);
			}
		}

		public void OnHasPartyUpdate(bool hasParty)
		{
			// Player left party
			if (!hasParty)
			{
				AutoSelectGameModeForTeamSize(1, true);
				return;
			}

			AutoSelectGameModeForTeamSize(_partyService.Members.Count);
		}

		private void AutoSelectGameModeForTeamSize(int size, bool forceMinSize = false)
		{
			if (SelectedGameMode.Value.Entry.TeamSize >= size && !forceMinSize)
			{
				// Already have a proper selected gamemode
				return;
			}

			var firstThatFits = _slots.OrderBy(g => g.Entry.TeamSize)
				.First(g => g.Entry.TeamSize >= size);

			SelectedGameMode.Value = firstThatFits;
		}

		private bool CanSelectGameMode(GameModeRotationConfig.GameModeEntry gameMode)
		{
			return IsInRotation(gameMode);
		}

		public bool IsInRotation(GameModeRotationConfig.GameModeEntry gameMode)
		{
			return _slots.Any(gmi => gmi.Entry == gameMode);
		}

		private void RefreshGameModes(bool forceAll)
		{
			for (var i = 0; i < _slots.Count; i++)
			{
				var slot = _slots[i];
				if (forceAll || !slot.IsFixed && slot.EndTime < DateTime.UtcNow)
				{
					RefreshSlot(i);
				}
			}
		}

		private void RefreshSlot(int index)
		{
			var entry = GetCurrentRotationEntry(index, out var ticksLeft, out var rotating);

			var info = new GameModeInfo(entry, rotating ? DateTime.UtcNow.AddTicks(ticksLeft) : default);
			_slots[index] = info;

			FLog.Info($"GameMode in slot {index} refreshed to {info.ToString()}");

			if (rotating)
			{
				var delay = (int) TimeSpan.FromTicks(ticksLeft).TotalMilliseconds + 500;
				_threadService.EnqueueDelayed(delay, () => 0, _ => { RefreshGameModes(false); });
			}
		}

		private GameModeRotationConfig.GameModeEntry GetCurrentRotationEntry(
			int slotIndex, out long ticksLeft, out bool rotating)
		{
			var config = _configsProvider.GetConfig<GameModeRotationConfig>();

			if (config.Slots[slotIndex].Count == 1)
			{
				rotating = false;
				ticksLeft = 0;
				return config.Slots[slotIndex][0];
			}

			var startTimeTicks = config.RotationStartTimeTicks;
			var slotDurationTicks = TimeSpan.FromSeconds(config.RotationSlotDuration).Ticks;

			var currentTime = DateTime.UtcNow.Ticks;

			var ticksFromStart = currentTime - startTimeTicks;
			var ticksWindow = slotDurationTicks * config.Slots[slotIndex].Count;
			var ticksElapsed = ticksFromStart % ticksWindow;

			var entryIndex = (int) Math.Ceiling((double) ticksElapsed / slotDurationTicks) - 1;
			ticksLeft = slotDurationTicks - ticksElapsed % slotDurationTicks;
			rotating = true;

			return config.Slots[slotIndex][entryIndex];
		}
	}
}