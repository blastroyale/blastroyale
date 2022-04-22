using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Ftue;
using FirstLight.Game.Utils;
using FirstLight.Game.Configs;
using FirstLight.Services;
using Photon.Realtime;
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
		
		/// <summary>
		/// Requests the last map that was played
		/// </summary>
		IObservableField<MapConfig> SelectedMap { get; }

		// TODO - Move to MatchLogic, once that functionality transitions to the backend
		/// <summary>
		/// Requests the current map config in timed rotation, for the selected game mode
		/// </summary>
		MapConfig CurrentMapConfigInRotation { get; }

		/// <summary>
		/// Requests the player's Nickname
		/// </summary>
		string Nickname { get; }
		
		/// <summary>
		/// Requests the player's Nickname
		/// </summary>
		IObservableFieldReader<string> NicknameId { get; }
	}

	/// <inheritdoc />
	public interface IAppLogic : IAppDataProvider
	{
		/// <summary>
		/// Requests and sets player nickname
		/// </summary>
		new IObservableField<string> NicknameId { get; }
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
		public IObservableField<MapConfig> SelectedMap { get; private set; }

		/// <inheritdoc />
		public string Nickname => NicknameId == null || string.IsNullOrWhiteSpace(NicknameId.Value) || NicknameId.Value.Length < 5 ?
			"" : NicknameId.Value.Substring(0, NicknameId.Value.Length - 5);

		/// <inheritdoc />
		IObservableFieldReader<string> IAppDataProvider.NicknameId => NicknameId;

		/// <inheritdoc />
		public IObservableField<string> NicknameId { get; private set; }
		
		/// <inheritdoc />
		public MapConfig CurrentMapConfigInRotation => GetCurrentMapConfig();

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

			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<MapConfig>();
			
			SelectedGameMode = new ObservableField<GameMode>(0);
			SelectedMap = new ObservableField<MapConfig>(configs[0]);
			NicknameId = new ObservableField<string>(Data.NickNameId);
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

		private MapConfig GetCurrentMapConfig()
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<MapConfig>();
			var compatibleMaps = new List<MapConfig>();
			var span = DateTime.UtcNow - DateTime.UtcNow.Date;
			var timeSegmentIndex = Mathf.RoundToInt((float) span.TotalMinutes / GameConstants.MAP_ROTATION_TIME_MINUTES);

			foreach (var config in configs)
			{
				// Filters compatible maps by game mode, and also filters out map ID 0
				// 0 is FTUE map, but it imports as a Deathmatc game modeh, so it shows up incorrectly for DM map list
				if (config.Value.GameMode == SelectedGameMode.Value && config.Value.Id > 0)
				{
					compatibleMaps.Add(config.Value);
				}
			}

			if (timeSegmentIndex >= compatibleMaps.Count)
			{
				timeSegmentIndex %= compatibleMaps.Count;
			}

			return compatibleMaps[timeSegmentIndex];
		}
	}
}