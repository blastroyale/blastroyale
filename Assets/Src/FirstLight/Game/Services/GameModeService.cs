using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Services;
using Quantum;

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
		IObservableField<SelectedGameModeInfo> SelectedGameMode { get; }

		/// <summary>
		/// The current rotational GameMode (updates automatically when time runs out, with a 500ms delay.
		/// </summary>
		IObservableFieldReader<GameModeRotationInfo> RotationGameMode { get; }
	}

	public struct SelectedGameModeInfo
	{
		public string Id;
		public MatchType MatchType;
		public List<string> Mutators;
		public DateTime EndTime;
		public bool FromRotation;

		public SelectedGameModeInfo(string id, MatchType matchType, List<string> mutators, bool fromRotation = false, DateTime endTime = default)
		{
			Id = id;
			MatchType = matchType;
			Mutators = mutators;
			FromRotation = fromRotation;
			EndTime = endTime;
		}
	}

	public struct GameModeRotationInfo
	{
		public GameModeRotationConfig.RotationEntry Entry;
		public DateTime EndTime;

		public GameModeRotationInfo(GameModeRotationConfig.RotationEntry entry, DateTime endTime)
		{
			Entry = entry;
			EndTime = endTime;
		}
	}

	/// <inheritdoc cref="IGameModeService"/>
	public class GameModeService : IGameModeService
	{
		private readonly IConfigsProvider _configsProvider;
		private readonly IThreadService _threadService;

		private readonly IObservableField<GameModeRotationInfo> _rotationGameMode;

		public IObservableField<SelectedGameModeInfo> SelectedGameMode { get; }
		public IObservableFieldReader<GameModeRotationInfo> RotationGameMode => _rotationGameMode;

		public GameModeService(IConfigsProvider configsProvider, IThreadService threadService)
		{
			_configsProvider = configsProvider;
			_threadService = threadService;

			SelectedGameMode = new ObservableField<SelectedGameModeInfo>();
			_rotationGameMode = new ObservableField<GameModeRotationInfo>();
		}

		public void Init()
		{
			SelectedGameMode.Value =
				new SelectedGameModeInfo(_configsProvider.GetConfigsList<QuantumGameModeConfig>()[0].Id,
				                         MatchType.Casual,
				                         new List<string>());

			RefreshRotationGameMode();
		}

		private void RefreshRotationGameMode()
		{
			var currentRotationEntry = GetCurrentRotationEntry(out var ticksLeft);
			_rotationGameMode.Value = new GameModeRotationInfo(currentRotationEntry, DateTime.UtcNow.AddTicks(ticksLeft));

			var delay = (int) TimeSpan.FromTicks(ticksLeft).TotalMilliseconds + 500;
			_threadService.EnqueueDelayed(delay, () => 0, _ => { RefreshRotationGameMode(); });
		}

		private GameModeRotationConfig.RotationEntry GetCurrentRotationEntry(out long ticksLeft)
		{
			var config = _configsProvider.GetConfig<GameModeRotationConfig>();

			var currentTime = DateTime.UtcNow.Ticks;
			var startTime = config.RotationStartTimeTicks;

			var ticksFromStart = currentTime - startTime;
			var slotDurationTicks = TimeSpan.FromSeconds(config.RotationSlotDuration).Ticks;
			var ticksWindow = slotDurationTicks * config.RotationEntries.Count;

			var ticksElapsed = ticksFromStart % ticksWindow;

			var index = 0;
			while (ticksElapsed > slotDurationTicks)
			{
				index++;
				ticksElapsed -= slotDurationTicks;
			}

			ticksLeft = slotDurationTicks - ticksElapsed;

			return config.RotationEntries[index];
		}
	}
}