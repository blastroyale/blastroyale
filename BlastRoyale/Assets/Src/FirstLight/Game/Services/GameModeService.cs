using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Configs.Utils;
using FirstLight.Game.Domains.HomeScreen;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.StateMachines;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Quantum;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace FirstLight.Game.Services
{
	public struct GameModeInfo
	{
		public IGameModeEntry Entry;
		public DurationConfig Duration;

		public bool IsFixed => Duration == null;

		public GameModeInfo(IGameModeEntry entry, DurationConfig duration = null)
		{
			Entry = entry;
			Duration = duration;
		}

		public string GetKey()
		{
			return Entry.MatchConfig.UniqueConfigId + ":" + Duration?.StartsAt + ":" + Duration?.EndsAt;
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
		IReadOnlyList<GameModeInfo> Slots { get; }

		TeamSizeConfig GetTeamSizeFor(IGameModeEntry entry);
		TeamSizeConfig GetTeamSizeFor(SimulationMatchConfig simulationMatchConfig);

		/// <summary>
		/// Check if the local player has seen a given event
		/// </summary>
		public bool HasSeenEvent(GameModeInfo info);

		/// <summary>
		/// Mark an event as seen
		/// </summary>
		public void MarkSeen(GameModeInfo info);

		/// <summary>
		/// Check if a given simulation match config is a valid event, it compares every field
		/// </summary>
		public bool IsInRotation(SimulationMatchConfig matchConfig);

		public void SelectValidGameMode();
	}

	/// <inheritdoc cref="IGameModeService"/>
	public class GameModeService : IGameModeService
	{
		private const int NextEventsDisplayDaysBefore = 3;
		private readonly IGameDataProvider _dataProvider;
		private readonly IGameCommandService _commandService;
		private readonly IConfigsProvider _configsProvider;
		private readonly IFLLobbyService _lobbyService;
		private readonly IAppDataProvider _appDataProvider;
		private readonly LocalPrefsService _localPrefsService;
		private readonly IRemoteTextureService _remoteTextureService;
		private readonly IHomeScreenService _homeScreenService;

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
						var mapConfig = _configsProvider.GetConfig<QuantumMapConfig>(mapId.ToString());
						if (!mapConfig.IsTestMap && !mapConfig.IsCustomOnly)
						{
							validMaps.Add(mapId);
						}
					}
				}

				return validMaps;
			}
		}

		public GameModeService(IGameDataProvider dataProvider, IGameCommandService commandService, IConfigsProvider configsProvider,
							   IFLLobbyService lobbyService,
							   IAppDataProvider appDataProvider, LocalPrefsService localPrefsService, IRemoteTextureService remoteTextureService,
							   IMessageBrokerService msgBroker, IHomeScreenService homeScreenService)
		{
			_dataProvider = dataProvider;
			_commandService = commandService;
			_configsProvider = configsProvider;
			_lobbyService = lobbyService;
			_appDataProvider = appDataProvider;
			_localPrefsService = localPrefsService;
			_remoteTextureService = remoteTextureService;
			_homeScreenService = homeScreenService;

			SelectedGameMode = new ObservableField<GameModeInfo>();
			SelectedGameMode.Observe(OnGameModeSet);
			_lobbyService.CurrentPartyCallbacks.LocalLobbyUpdated += OnPartyLobbyChanged;
			_lobbyService.CurrentPartyCallbacks.LocalLobbyJoined += OnPartyLocalLobbyJoined;
			;
			msgBroker.Subscribe<MatchmakingLeftMessage>(OnMatchmakingLeft);
			msgBroker.Subscribe<CoreLoopInitialized>(OnCoreLoopInitialized);
			msgBroker.Subscribe<MainMenuOpenedMessage>(OnMainMenuOpened);
			msgBroker.Subscribe<GameCompletedRewardsMessage>(OnGameRewards);
			homeScreenService.CustomPlayButtonValidations += ValidatePlayButton;
		}

		private void ValidatePlayButton(List<string> errors)
		{
			if ((_lobbyService.CurrentPartyLobby?.Players?.Count ?? 1) >
				SelectedGameMode.Value.Entry.MatchConfig.TeamSize)
			{
				errors.Add("Invalid party size!");
				return;
			}

			if (_lobbyService.CurrentPartyLobby != null && _lobbyService.CurrentPartyLobby.IsLocalPlayerHost() &&
				!_lobbyService.CurrentPartyLobby.IsEveryoneReady())
			{
				errors.Add("Waiting for team members to be ready!");
				return;
			}

			if (SelectedGameMode.Value.Entry is EventGameModeEntry ev && ev.IsPaid &&
				!_dataProvider.GameEventsDataProvider.HasPass(ev.MatchConfig.UniqueConfigId))
			{
				errors.Add("You need a pass to play this event. Try selecting it again!");
				SelectValidGameMode();
			}
		}

		/// <summary>
		/// Player got kicked by being afk so did not use the ticket, lets refund
		/// </summary>
		private void OnGameRewards(GameCompletedRewardsMessage obj)
		{
			if (!obj.Rewards.UsedEventPass && obj.Rewards.SimulationConfigId != null &&
				_dataProvider.GameEventsDataProvider.HasPass(obj.Rewards.SimulationConfigId))
			{
				_commandService.ExecuteCommand(new RefundEventPassesCommand());
			}
		}

		/// <summary>
		/// Change back the gamemode to a not paid one when going back to the home screen
		/// </summary>
		private void OnMainMenuOpened(MainMenuOpenedMessage msg)
		{
			if (SelectedGameMode.Value.Entry is EventGameModeEntry ev && ev.IsPaid)
			{
				if (!_dataProvider.GameEventsDataProvider.HasPass(ev.MatchConfig.UniqueConfigId))
				{
					SelectValidGameMode();
				}
			}
		}

		/// <summary>
		/// When opening the game refund tickets if the player is not reconnecting to a match
		/// </summary>
		private void OnCoreLoopInitialized(CoreLoopInitialized msg)
		{
			if (msg.ConnectedToMatch) return; // if player is reconnecting we don't want to remove the ticket he may use
			if (_dataProvider.GameEventsDataProvider.HasAnyPass())
			{
				_commandService.ExecuteCommand(new RefundEventPassesCommand());
			}
		}

		/// <summary>
		/// If player was in a paid event change to default gamemode and let him go again through the menu of selecting it
		/// And also refund the pass price
		/// </summary>
		private void OnMatchmakingLeft(MatchmakingLeftMessage msg)
		{
			if (SelectedGameMode.Value.Entry is EventGameModeEntry ev && ev.IsPaid)
			{
				if (_dataProvider.GameEventsDataProvider.HasPass(ev.MatchConfig.UniqueConfigId))
				{
					_commandService.ExecuteCommand(new RefundEventPassesCommand());
				}

				SelectValidGameMode();
			}
		}

		public void Init()
		{
			foreach (var uniqueUrls in Slots.Where(slot => slot.Entry is EventGameModeEntry)
						 .Select(slot => ((EventGameModeEntry) slot.Entry).ImageURL).Distinct())
			{
				if (uniqueUrls == null) continue;
				_remoteTextureService.RequestTexture(uniqueUrls).Forget();
			}

			CheckEventEnd().Forget();
			if (!string.IsNullOrEmpty(_localPrefsService.SelectedGameMode.Value))
			{
				var configId = _localPrefsService.SelectedGameMode.Value;
				var firstOrDefault =
					Slots.FirstOrDefault(a => a.Entry.MatchConfig.UniqueConfigId == configId && IsInRotation(a.Entry) && !IsPaid(a.Entry));
				if (firstOrDefault.Entry != null)
				{
					SelectedGameMode.Value = firstOrDefault;
					return;
				}
			}

			this.SelectDefaultRankedMode();
		}

		public async UniTaskVoid CheckEventEnd()
		{
			while (true)
			{
				await UniTask.WaitForSeconds(5);
				if (!IsInRotation(SelectedGameMode.Value.Entry))
				{
					if (_lobbyService.CurrentPartyLobby != null && _lobbyService.CurrentPartyLobby.IsLocalPlayerHost())
					{
						AutoSelectGameModeForTeamSize(_lobbyService.CurrentPartyLobby.Players.Count, true);
					}

					if (_lobbyService.CurrentPartyLobby == null)
					{
						AutoSelectGameModeForTeamSize(1, true);
					}
				}
			}
		}

		private void OnGameModeSet(GameModeInfo _, GameModeInfo current)
		{
			if (current.Entry == null)
			{
				_localPrefsService.SelectedGameMode.Value = null;
				return;
			}

			FLog.Info($"Selected GameMode set to: {current}");

			_localPrefsService.SelectedGameMode.Value = current.Entry.MatchConfig.UniqueConfigId;

			if (_lobbyService.CurrentPartyLobby != null && _lobbyService.CurrentPartyLobby.IsLocalPlayerHost())
			{
				// TODO: Should wait for this or something
				_lobbyService.UpdatePartyMatchmakingGameMode(current.Entry.MatchConfig.UniqueConfigId).Forget();
			}
		}

		private void OnPartyLocalLobbyJoined(Lobby lobby)
		{
			if (!lobby.IsLocalPlayerHost())
			{
				SelectedGameMode.Value =
					Slots.FirstOrDefault(a => a.Entry.MatchConfig.UniqueConfigId == lobby.Data[FLLobbyService.KEY_MATCHMAKING_GAMEMODE].Value);
			}
		}

		private void OnPartyLobbyChanged(ILobbyChanges changes)
		{
			if (changes == null || changes.LobbyDeleted) return;

			if (changes.Data.Changed && changes.Data.Value.TryGetValue(FLLobbyService.KEY_MATCHMAKING_GAMEMODE, out var gameModeConfig))
			{
				SelectedGameMode.Value = Slots.FirstOrDefault(a => a.Entry.MatchConfig.UniqueConfigId == gameModeConfig.Value.Value);
			}

			if (_lobbyService.CurrentPartyLobby.IsLocalPlayerHost() && (changes.PlayerJoined.Changed || changes.PlayerLeft.Changed))
			{
				AutoSelectGameModeForTeamSize(_lobbyService.CurrentPartyLobby.Players.Count);
			}
		}

		public bool IsInRotation(SimulationMatchConfig matchConfig)
		{
			var match = matchConfig.ToByteArray();
			foreach (var gameModeInfo in Slots)
			{
				if (IsInRotation(gameModeInfo.Entry) && matchConfig.UniqueConfigId == gameModeInfo.Entry.MatchConfig.UniqueConfigId)
				{
					return true;
				}
			}

			return false;
		}

		public void SelectValidGameMode()
		{
			if (_lobbyService.IsInPartyLobby())
			{
				AutoSelectGameModeForTeamSize(_lobbyService.CurrentPartyLobby.Players.Count);
				return;
			}

			AutoSelectGameModeForTeamSize(1);
		}

		public bool IsInRotation(IGameModeEntry gameMode)
		{
			if (gameMode is FixedGameModeEntry)
			{
				return true;
			}

			if (gameMode is EventGameModeEntry ev)
			{
				var now = DateTime.UtcNow;
				return ev.Schedule.Any(a => a.Contains(now));
			}

			return false;
		}

		public bool IsPaid(IGameModeEntry gamemode)
		{
			return gamemode is EventGameModeEntry ev && ev.IsPaid;
		}

		private void AutoSelectGameModeForTeamSize(int size, bool forceMinSize = false)
		{
			if (SelectedGameMode.Value.Entry != null
				&& !IsPaid(SelectedGameMode.Value.Entry)
				&& SelectedGameMode.Value.Entry.MatchConfig.TeamSize >= size
				&& !forceMinSize)
			{
				// Already have a proper selected gamemode
				return;
			}

			var firstThatFits = Slots
				.Where(a => a.Entry.MatchConfig != null && a.Entry is FixedGameModeEntry)
				.OrderBy(g => g.Entry.MatchConfig.TeamSize)
				.First(g => g.Entry.MatchConfig.TeamSize >= size);

			SelectedGameMode.Value = firstThatFits;
		}

		public IReadOnlyList<GameModeInfo> Slots
		{
			get
			{
				var data = MainInstaller.ResolveData().RemoteConfigProvider;
				var slots = new List<GameModeInfo>();
				var eventConfigs = data.GetConfig<EventGameModesConfig>();
				var fixedSlotsConfig = data.GetConfig<FixedGameModesConfig>();
				slots.AddRange(GetEvents(eventConfigs));
				slots.AddRange(fixedSlotsConfig.Select(f => new GameModeInfo(f)));
				return slots;
			}
		}

		public TeamSizeConfig GetTeamSizeFor(IGameModeEntry entry)
		{
			var fixedConfig = MainInstaller.ResolveData().RemoteConfigProvider.GetConfig<MatchmakingQueuesConfig>();
			return fixedConfig[entry.MatchConfig.TeamSize.ToString()];
		}

		public TeamSizeConfig GetTeamSizeFor(SimulationMatchConfig simulationMatchConfig)
		{
			var fixedConfig = MainInstaller.ResolveData().RemoteConfigProvider.GetConfig<MatchmakingQueuesConfig>();
			return fixedConfig[simulationMatchConfig.TeamSize.ToString()];
		}

		public bool HasSeenEvent(GameModeInfo info)
		{
			var value = _localPrefsService.SeenEvents.Value;
			return value != null && value.Contains(info.GetKey());
		}

		public void MarkSeen(GameModeInfo info)
		{
			var seen = _localPrefsService.SeenEvents.Value;
			seen.Add(info.GetKey());
			_localPrefsService.SeenEvents.Value = seen;
		}

		private List<GameModeInfo> GetEvents(EventGameModesConfig config)
		{
			var events = new List<GameModeInfo>();
			var now = DateTime.UtcNow;

			EventGameModeEntry closest = default;
			DurationConfig closestDate = null;
			foreach (var gameModeEntry in config)
			{
				foreach (var timedGameModeEntry in gameModeEntry.Schedule)
				{
					if (timedGameModeEntry.Contains(now))
					{
						events.Add(new GameModeInfo(gameModeEntry, timedGameModeEntry));
						break;
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

			if (events.Count == 0 && closestDate != null)
			{
				events.Add(new GameModeInfo(closest, closestDate));
			}

			return events;
		}
	}
}