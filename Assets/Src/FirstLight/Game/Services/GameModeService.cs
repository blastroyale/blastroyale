using System;
using FirstLight.Game.Configs;
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
		/// The currently selected GameMode.
		/// </summary>
		IObservableField<string> SelectedGameModeId { get; }

		/// <summary>
		/// The currently selected Map ID.
		/// </summary>
		IObservableField<string> SelectedMapId { get; }

		/// <summary>
		/// The current rotational GameMode (updates automatically when time runs out, with a 500ms delay.
		/// </summary>
		IObservableFieldReader<GameModeRotationConfig.RotationEntry> RotationGameMode { get; }
	}

	/// <inheritdoc cref="IGameModeService"/>
	public class GameModeService : IGameModeService
	{
		private readonly IConfigsProvider _configsProvider;
		private readonly IThreadService _threadService;

		private IObservableField<GameModeRotationConfig.RotationEntry> _rotationGameMode;

		public IObservableField<string> SelectedGameModeId { get; }
		public IObservableField<string> SelectedMapId { get; }
		public IObservableFieldReader<GameModeRotationConfig.RotationEntry> RotationGameMode => _rotationGameMode;

		public GameModeService(IConfigsProvider configsProvider, IThreadService threadService)
		{
			_configsProvider = configsProvider;
			_threadService = threadService;

			_configsProvider.GetConfig<GameModeRotationConfig>();

			SelectedGameModeId = new ObservableField<string>(null); // TODO: Set this!
			SelectedMapId = new ObservableField<string>(null); // TODO: Set this!
			_rotationGameMode = new ObservableField<GameModeRotationConfig.RotationEntry>();

			RefreshRotationGameMode();
		}

		private void RefreshRotationGameMode()
		{
			var currentRotationEntry = GetCurrentRotationEntry(out int index, out long ticksLeft);
			_rotationGameMode.Value = currentRotationEntry;

			var delay = (int) TimeSpan.FromTicks(ticksLeft).TotalMilliseconds + 500;
			_threadService.EnqueueDelayed(delay, () => 0, _ => { RefreshRotationGameMode(); });
		}

		private GameModeRotationConfig.RotationEntry GetCurrentRotationEntry(out int index, out long ticksLeft)
		{
			var config = _configsProvider.GetConfig<GameModeRotationConfig>();

			var currentTime = DateTime.UtcNow.Ticks;
			var startTime = config.StartTimeTicks;

			var ticksFromStart = currentTime - startTime;
			var slotDurationTicks = TimeSpan.FromSeconds(config.SlotDuration).Ticks;
			var ticksWindow = slotDurationTicks * config.Entries.Count;

			var ticksElapsed = ticksFromStart % ticksWindow;

			index = 0;
			while (ticksElapsed > slotDurationTicks)
			{
				index++;
				ticksElapsed -= slotDurationTicks;
			}

			ticksLeft = slotDurationTicks - ticksElapsed;

			return config.Entries[index];
		}
	}
}