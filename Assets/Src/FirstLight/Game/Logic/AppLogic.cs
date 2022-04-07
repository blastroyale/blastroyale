using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Ftue;
using FirstLight.Game.Utils;
using FirstLight.Game.Configs;
using FirstLight.Services;
using Quantum;
using UnityEngine;


namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the game's app
	/// </summary>
	public interface IAppDataProvider
	{
		/// <summary>
		/// Requests the information if the current game session is the first time the player is playing the game or not
		/// </summary>
		bool IsFirstSession { get; }

		/// <summary>
		/// Requests the information if the game was or not yet reviewed
		/// </summary>
		bool IsGameReviewed { get; }

		/// <summary>
		/// Are Sound Effects enabled?
		/// </summary>
		bool IsSfxOn { get; set; }

		/// <summary>
		/// Is Background Music enabled?
		/// </summary>
		bool IsBgmOn { get; set; }

		/// <summary>
		/// Is Haptic feedback on device enabled?
		/// </summary>
		bool IsHapticOn { get; set; }

		/// <summary>
		/// Marks the date when the game was last time reviewed
		/// </summary>
		void MarkGameAsReviewed();

		// TODO - Move to MatchLogic, once that functionality transitions to the backend
		/// <summary>
		/// Requests the current selected game mode <see cref="GameMode"/>.
		/// </summary>
		IObservableField<GameMode> SelectedGameMode { get; }

		// TODO - Move to MatchLogic, once that functionality transitions to the backend
		/// <summary>
		/// Requests the current map config in timed rotation, for the selected game mode
		/// </summary>
		MapConfig CurrentMapConfig { get; }
	}

	/// <inheritdoc />
	public interface IAppLogic : IAppDataProvider
	{
	}

	/// <inheritdoc cref="IAppLogic"/>
	public class AppLogic : AbstractBaseLogic<AppData>, IAppLogic, IGameLogicInitializer
	{
		private readonly DateTime _defaultZeroTime = new DateTime(2020, 1, 1);
		private readonly IAudioFxService<AudioId> _audioFxService;
		
		/// <inheritdoc />
		public bool IsFirstSession => Data.IsFirstSession;

		/// <inheritdoc />
		public bool IsGameReviewed => Data.GameReviewDate > _defaultZeroTime;

		/// <inheritdoc />
		public bool IsSfxOn
		{
			get => Data.SfxEnabled;
			set
			{
				Data.SfxEnabled = value;
				_audioFxService.Is2dSfxMuted = !value;
				_audioFxService.Is3dSfxMuted = !value;
			}
		}

		/// <inheritdoc />
		public bool IsBgmOn
		{
			get => Data.BgmEnabled;
			set
			{
				Data.BgmEnabled = value;
				_audioFxService.IsBgmMuted = !value;
			}
		}

		/// <inheritdoc />
		public bool IsHapticOn
		{
			get => Data.HapticEnabled;
			set => Data.HapticEnabled = value;
		}

		/// <inheritdoc />
		public IObservableField<GameMode> SelectedGameMode { get; private set; }
		
		/// <inheritdoc />
		public MapConfig CurrentMapConfig
		{
			get
			{
				var compatibleMaps = new List<MapConfig>();

				// Filters compatible maps by game mode, and also filters out map ID 0
				// 0 is FTUE map, but it imports as a Deathmatc game modeh, so it shows up incorrectly for DM map list
				compatibleMaps.AddRange(GameLogic.ConfigsProvider.GetConfigsList<MapConfig>()
				                                 .Where(x => x.GameMode == SelectedGameMode.Value && x.Id > 0));

				var morning = DateTime.Today;
				var now = DateTime.UtcNow;
				var span = now - morning;
				var timeSegmentIndex = Mathf.RoundToInt((float) span.TotalMinutes / GameConstants.MAP_ROTATION_TIME_MINUTES);

				if (timeSegmentIndex >= compatibleMaps.Count)
				{
					timeSegmentIndex -= (compatibleMaps.Count * (timeSegmentIndex / compatibleMaps.Count));
				}

				return compatibleMaps[timeSegmentIndex];
			}
		}

		public AppLogic(IGameLogic gameLogic, IDataProvider dataProvider, IAudioFxService<AudioId> audioFxService) :
			base(gameLogic, dataProvider)
		{
			_audioFxService = audioFxService;
		}

		/// <inheritdoc />
		public void Init()
		{
			IsSfxOn = IsSfxOn;
			IsBgmOn = IsBgmOn;

			SelectedGameMode = new ObservableField<GameMode>(0);
		}

		/// <inheritdoc />
		public void MarkGameAsReviewed()
		{
			if (IsGameReviewed)
			{
				throw new LogicException("The game was already reviewed and cannot be reviewed again");
			}

			Data.GameReviewDate = GameLogic.TimeService.DateTimeUtcNow;
		}
	}
}