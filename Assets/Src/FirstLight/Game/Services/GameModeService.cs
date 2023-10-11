using System;
using System.Collections.Generic;
using System.Linq;
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

		public GameModeInfo(string gameModeId, MatchType matchType, List<string> mutators, bool isSquads, bool needNft, List<GameId> allowedRewards,
							DateTime endTime = default)
		{
			Entry = new GameModeRotationConfig.GameModeEntry(gameModeId, matchType, mutators, isSquads, needNft, allowedRewards);
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
		private readonly IConfigsProvider _configsProvider;
		private readonly IThreadService _threadService;
		private readonly IPartyService _partyService;
		private readonly IEquipmentDataProvider _equipmentDataProvider;
		private readonly IAppDataProvider _appDataProvider;

		private readonly IObservableList<GameModeInfo> _slots;

		public IObservableField<GameModeInfo> SelectedGameMode { get; }

		public IObservableListReader<GameModeInfo> Slots => _slots;


		public GameModeService(IConfigsProvider configsProvider, IThreadService threadService,
							   IEquipmentDataProvider equipmentDataProvider, IPartyService partyService,
							   IAppDataProvider appDataProvider)
		{
			_configsProvider = configsProvider;
			_threadService = threadService;
			_equipmentDataProvider = equipmentDataProvider;
			_partyService = partyService;
			_appDataProvider = appDataProvider;

			_slots = new ObservableList<GameModeInfo>(new List<GameModeInfo>());
			SelectedGameMode = new ObservableField<GameModeInfo>();
			SelectedGameMode.Observe(OnGameModeSet);
			_partyService.HasParty.Observe((_, _) => { OnPartyUpdate(); });
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

		public void OnPartyUpdate()
		{
			bool hasParty = _partyService.HasParty.Value;
			if (hasParty && !SelectedGameMode.Value.Entry.Squads)
			{
				SelectedGameMode.Value = FindModeWithSquads(true);
				return;
			}

			// If the player have NFT he can play squads alone so there is no need to change back
			if (!CanSelectGameMode(SelectedGameMode.Value.Entry))
			{
				this.SelectDefaultRankedMode();
			}
		}

		private bool CanSelectGameMode(GameModeRotationConfig.GameModeEntry gameMode)
		{
			bool hasParty = _partyService.HasParty.Value;
			if (gameMode.Squads && !hasParty && !_equipmentDataProvider.HasNfts())
			{
				return false;
			}

			return IsInRotation(gameMode);
		}

		private GameModeInfo FindModeWithSquads(bool squads)
		{
			return _slots.First(gm => gm.Entry.Squads == squads);
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