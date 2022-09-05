using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Server.SDK.Modules.GameConfiguration;
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
		/// Provides a list of fixed game mode slots that should always be displayed.
		/// </summary>
		List<GameModeRotationConfig.GameModeEntry> FixedSlots { get; }

		/// <summary>
		/// The current rotational GameMode in slot 1 (updates automatically when time runs out, with a 500ms delay.
		/// </summary>
		IObservableFieldReader<GameModeRotationInfo> RotationSlot1 { get; }

		/// <summary>
		/// The current rotational GameMode in slot 2 (updates automatically when time runs out, with a 500ms delay.
		/// </summary>
		IObservableFieldReader<GameModeRotationInfo> RotationSlot2 { get; }
	}

	public struct SelectedGameModeInfo
	{
		public string Id;
		public MatchType MatchType;
		public List<string> Mutators;
		public DateTime EndTime;
		public bool FromRotation;

		public SelectedGameModeInfo(string id, MatchType matchType, List<string> mutators, bool fromRotation = false,
		                            DateTime endTime = default)
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
		public GameModeRotationConfig.GameModeEntry Entry;
		public DateTime EndTime;

		public GameModeRotationInfo(GameModeRotationConfig.GameModeEntry entry, DateTime endTime)
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

		private readonly IObservableField<GameModeRotationInfo> _rotationSlot1;
		private readonly IObservableField<GameModeRotationInfo> _rotationSlot2;

		public IObservableField<SelectedGameModeInfo> SelectedGameMode { get; }

		public List<GameModeRotationConfig.GameModeEntry> FixedSlots =>
			_configsProvider.GetConfig<GameModeRotationConfig>().FixedSlots;

		public IObservableFieldReader<GameModeRotationInfo> RotationSlot1 => _rotationSlot1;
		public IObservableFieldReader<GameModeRotationInfo> RotationSlot2 => _rotationSlot2;

		public GameModeService(IConfigsProvider configsProvider, IThreadService threadService)
		{
			_configsProvider = configsProvider;
			_threadService = threadService;

			SelectedGameMode = new ObservableField<SelectedGameModeInfo>();
			_rotationSlot1 = new ObservableField<GameModeRotationInfo>();
			_rotationSlot2 = new ObservableField<GameModeRotationInfo>();
		}

		public void Init()
		{
			var firstFixedSlot = _configsProvider.GetConfig<GameModeRotationConfig>().FixedSlots[0];
			SelectedGameMode.Value = new SelectedGameModeInfo(firstFixedSlot.GameModeId,
			                                                  firstFixedSlot.MatchType,
			                                                  firstFixedSlot.Mutators);

			RefreshRotationGameMode();
		}

		private void RefreshRotationGameMode()
		{
			var config = _configsProvider.GetConfig<GameModeRotationConfig>();
			var startTimeTicks = config.RotationStartTimeTicks;
			var slotDurationTicks = TimeSpan.FromSeconds(config.RotationSlotDuration).Ticks;

			// Slot1
			RefreshRotationSlot(startTimeTicks, slotDurationTicks, config.RotationSlot2, _rotationSlot2);

			// Slot2
			RefreshRotationSlot(startTimeTicks, slotDurationTicks, config.RotationSlot1, _rotationSlot1);
		}

		private void RefreshRotationSlot(long startTimeTicks, long slotDurationTicks,
		                                 List<GameModeRotationConfig.GameModeEntry> rotationSlots,
		                                 IObservableField<GameModeRotationInfo> observable)
		{
			if (observable.Value.EndTime > DateTime.UtcNow) return;

			var currentRotationEntry =
				GetCurrentRotationEntry(startTimeTicks, slotDurationTicks, rotationSlots, out var ticksLeft);
			observable.Value = new GameModeRotationInfo(currentRotationEntry, DateTime.UtcNow.AddTicks(ticksLeft));

			var delay = (int) TimeSpan.FromTicks(ticksLeft).TotalMilliseconds + 500;
			_threadService.EnqueueDelayed(delay, () => 0, _ => { RefreshRotationGameMode(); });
		}

		private GameModeRotationConfig.GameModeEntry GetCurrentRotationEntry(
		long startTimeTicks, long slotDurationTicks, List<GameModeRotationConfig.GameModeEntry> rotationSlots,
		out long ticksLeft)
		{
			var currentTime = DateTime.UtcNow.Ticks;

			var ticksFromStart = currentTime - startTimeTicks;
			var ticksWindow = slotDurationTicks * rotationSlots.Count;
			var ticksElapsed = ticksFromStart % ticksWindow;

			var index = (int) Math.Ceiling((double) ticksElapsed / slotDurationTicks) - 1;
			ticksLeft = slotDurationTicks - ticksElapsed % slotDurationTicks;

			return rotationSlots[index];
		}
	}
}