using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;

namespace FirstLight.Game.Services
{
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
	}

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

		public GameModeInfo(string gameModeId, MatchType matchType, List<string> mutators, DateTime endTime = default)
		{
			Entry = new GameModeRotationConfig.GameModeEntry(gameModeId, matchType, mutators);
			EndTime = endTime;
		}

		public override string ToString()
		{
			return $"Entry({Entry}), EndTime({EndTime}), IsFixed({IsFixed})";
		}
	}

	/// <inheritdoc cref="IGameModeService"/>
	public class GameModeService : IGameModeService
	{
		private readonly IConfigsProvider _configsProvider;
		private readonly IThreadService _threadService;

		private readonly IObservableList<GameModeInfo> _slots;

		public IObservableField<GameModeInfo> SelectedGameMode { get; }

		public IObservableListReader<GameModeInfo> Slots => _slots;

		public GameModeService(IConfigsProvider configsProvider, IThreadService threadService)
		{
			_configsProvider = configsProvider;
			_threadService = threadService;

			_slots = new ObservableList<GameModeInfo>(new List<GameModeInfo>());
			SelectedGameMode = new ObservableField<GameModeInfo>();
			SelectedGameMode.Observe((_, gm) => FLog.Info($"Selected GameMode set to: {gm}"));
		}

		public void Init()
		{
			var config = _configsProvider.GetConfig<GameModeRotationConfig>();
			var firstGameMode = config.Slots[0][0];
			SelectedGameMode.Value = new GameModeInfo(firstGameMode);

			// Initially add empty objects which get updated by RefreshGameModes
			for (var i = 0; i < config.Slots.Count; i++)
			{
				_slots.Add(default);
			}

			RefreshGameModes(true);
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