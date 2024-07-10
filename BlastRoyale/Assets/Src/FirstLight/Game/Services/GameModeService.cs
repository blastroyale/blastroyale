using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Utils;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services.Party;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Quantum;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace FirstLight.Game.Services
{
	public struct GameModeInfo
	{
		public GameModeRotationConfig.GameModeEntry Entry;
		public DurationConfig Duration;

		public bool IsFixed => Duration == null;

		public GameModeInfo(GameModeRotationConfig.GameModeEntry entry, DurationConfig duration = null)
		{
			Entry = entry;
			Duration = duration;
		}

		public string GetKey()
		{
			return Entry.MatchConfig.ConfigId + ":" + Duration?.StartsAt + ":" + Duration?.EndsAt;
		}

		public override string ToString()
		{
			return $"Entry({Entry}), EndTime({Duration?.EndsAt}), IsFixed({IsFixed})";
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
		void Init(GameModeRotationConfig config = default);

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

		/// <summary>
		/// Returns the closer event in the configs 
		/// </summary>
		public bool TryGetNextEvent(out GameModeInfo next);
	}

	/// <inheritdoc cref="IGameModeService"/>
	public class GameModeService : IGameModeService
	{
		private const string SelectedQueueLobbyProperty = "selected_queue";

		private const int NextEventsDisplayDaysBefore = 3;
		private readonly IConfigsProvider _configsProvider;
		private readonly IThreadService _threadService;
		private readonly FLLobbyService _lobbyService;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IAppDataProvider _appDataProvider;
		private readonly LocalPrefsService _localPrefsService;
		private readonly IRemoteTextureService _remoteTextureService;

		private readonly IObservableList<GameModeInfo> _slots;
		private GameId _selectedMap;

		public IObservableField<GameModeInfo> SelectedGameMode { get; }

		public GameId SelectedMap
		{
			set => _localPrefsService.SelectedRankedMap.Value = (int) value;
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
							   IGameDataProvider gameDataProvider, FLLobbyService lobbyService,
							   IAppDataProvider appDataProvider, LocalPrefsService localPrefsService, IRemoteTextureService remoteTextureService)
		{
			_configsProvider = configsProvider;
			_threadService = threadService;
			_gameDataProvider = gameDataProvider;
			_lobbyService = lobbyService;
			_appDataProvider = appDataProvider;
			_localPrefsService = localPrefsService;
			_remoteTextureService = remoteTextureService;

			_slots = new ObservableList<GameModeInfo>(new List<GameModeInfo>());
			SelectedGameMode = new ObservableField<GameModeInfo>();
			SelectedGameMode.Observe(OnGameModeSet);

			_lobbyService.CurrentPartyCallbacks.LobbyJoined += OnPartyLobbyJoined;
			_lobbyService.CurrentPartyCallbacks.LobbyChanged += OnPartyLobbyChanged;
		}

		public void Init(GameModeRotationConfig config = default)
		{
			if (config.Slots == null)
			{
				config = _configsProvider.GetConfig<GameModeRotationConfig>();
			}

			_slots.Clear();
			// Initially add empty objects which get updated by RefreshGameModes
			for (var i = 0; i < config.Slots.Count; i++)
			{
				_slots.Add(default);
			}

			RefreshGameModes(true);

			// Try to set the saved game mode
			var lastGameMode = _localPrefsService.SelectedGameMode.Value;
			if (!string.IsNullOrEmpty(lastGameMode))
			{
				foreach (var gm in _slots)
				{
					if (gm.Entry.MatchConfig == null) continue;
					if (gm.Entry.MatchConfig.ConfigId == lastGameMode && IsInRotation(gm.Entry))
					{
						FLog.Verbose($"Restored selected game mode to: {lastGameMode}");
						SelectedGameMode.Value = gm;
						return;
					}
				}
			}

			this.SelectDefaultRankedMode();
		}

		private void OnGameModeSet(GameModeInfo _, GameModeInfo current)
		{
			FLog.Info($"Selected GameMode set to: {current}");

			_localPrefsService.SelectedGameMode.Value = current.Entry.MatchConfig.ConfigId;
			if (_appDataProvider.IsPlayerLoggedIn)
			{
				MainInstaller.Resolve<IGameServices>().DataSaver.SaveData<AppData>();
			}

			if (_lobbyService.CurrentPartyLobby != null && _lobbyService.CurrentPartyLobby.IsLocalPlayerHost())
			{
				// TODO: Should wait for this or something
				_lobbyService.UpdatePartyMatchmakingQueue(current.Entry.PlayfabQueue.QueueName).Forget();
			}
		}

		private void OnPartyLobbyJoined(Lobby lobby)
		{
			if (!lobby.IsLocalPlayerHost())
			{
				SelectedGameMode.Value =
					_slots.FirstOrDefault(a => a.Entry.PlayfabQueue.QueueName == lobby.Data[FLLobbyService.KEY_MATCHMAKING_QUEUE].Value);
			}
		}

		private void OnPartyLobbyChanged(ILobbyChanges changes)
		{
			if (changes.LobbyDeleted) return;

			if (changes.Data.Changed && changes.Data.Value.TryGetValue(FLLobbyService.KEY_MATCHMAKING_QUEUE, out var queue))
			{
				SelectedGameMode.Value = _slots.FirstOrDefault(a => a.Entry.PlayfabQueue.QueueName == queue.Value.Value);
			}

			if (_lobbyService.CurrentPartyLobby.IsLocalPlayerHost() && (changes.PlayerJoined.Changed || changes.PlayerLeft.Changed))
			{
				AutoSelectGameModeForTeamSize(_lobbyService.CurrentPartyLobby.Players.Count);
			}
		}

		public bool IsInRotation(GameModeRotationConfig.GameModeEntry gameMode)
		{
			if (!gameMode.TimedEntry)
			{
				return true;
			}

			var now = DateTime.UtcNow;
			return gameMode.TimedGameModeEntries.Any(a => a.Contains(now));
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

		private void AutoSelectGameModeForTeamSize(int size, bool forceMinSize = false)
		{
			if (SelectedGameMode.Value.Entry.TeamSize >= size && !forceMinSize)
			{
				// Already have a proper selected gamemode
				return;
			}

			var firstThatFits = _slots
				.Where(a => a.Entry.MatchConfig != null && IsInRotation(a.Entry))
				.OrderBy(g => g.Entry.TeamSize)
				.First(g => g.Entry.TeamSize >= size);

			SelectedGameMode.Value = firstThatFits;
		}

		private void RefreshGameModes(bool forceAll)
		{
			for (var i = 0; i < _slots.Count; i++)
			{
				var slot = _slots[i];
				if (forceAll || !slot.IsFixed && slot.Duration.GetEndsAtDateTime() < DateTime.UtcNow)
				{
					RefreshSlot(i);
				}
			}
		}

		private void RefreshSlot(int index)
		{
			if (!TryGetGameMode(index, out var entry, out var duration))
			{
				_slots[index] = default;
				return;
			}

			var info = new GameModeInfo(entry, duration);
			_slots[index] = info;

			FLog.Info($"GameMode in slot {index} refreshed to {info.ToString()}");

			if (!info.IsFixed)
			{
				var diff = (duration.GetEndsAtDateTime() - DateTime.UtcNow).Add(TimeSpan.FromSeconds(1));
				UpdateGameModes(diff).Forget();
			}

			// Cache gamemode image on game load
			if (!string.IsNullOrWhiteSpace(info.Entry.Visual.OverwriteImageURL))
			{
				_remoteTextureService.RequestTexture(info.Entry.Visual.OverwriteImageURL).Forget();
			}
		}

		private async UniTaskVoid UpdateGameModes(TimeSpan delay)
		{
			await UniTask.Delay(delay);
			RefreshGameModes(false);
		}

		public bool TryGetNextEvent(out GameModeInfo next)
		{
			for (var i = 0; i < _slots.Count; i++)
			{
				if (TryGetGameMode(i, out var entry, out var duration))
				{
					if (entry.TimedEntry)
					{
						next = new GameModeInfo() {Duration = duration, Entry = entry};
						return true;
					}
				}
			}

			next = default;
			return false;
		}

		private bool TryGetGameMode(
			int slotIndex, out GameModeRotationConfig.GameModeEntry entry, out DurationConfig duration)
		{
			var config = _configsProvider.GetConfig<GameModeRotationConfig>();
			var now = DateTime.UtcNow;

			GameModeRotationConfig.GameModeEntry closest = default;
			DurationConfig closestDate = null;
			foreach (var gameModeEntry in config.Slots[slotIndex].Entries)
			{
				if (!gameModeEntry.TimedEntry)
				{
					duration = null;
					entry = gameModeEntry;
					return true;
				}

				foreach (var timedGameModeEntry in gameModeEntry.TimedGameModeEntries)
				{
					if (timedGameModeEntry.Contains(now))
					{
						duration = timedGameModeEntry;
						entry = gameModeEntry;
						return true;
					}

					var starts = timedGameModeEntry.GetStartsAtDateTime();
					if (starts > now
						&& (closestDate == null || starts < closestDate.GetStartsAtDateTime())
						&& starts <= now.AddDays(NextEventsDisplayDaysBefore))
					{
						closestDate = timedGameModeEntry;
						closest = gameModeEntry;
					}
				}
			}

			if (closestDate != null)
			{
				entry = closest;
				duration = closestDate;
				return true;
			}

			duration = null;
			entry = default;
			return false;
		}
	}
}
