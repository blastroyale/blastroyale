using System;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
using FirstLight.Services;
using MoreMountains.NiceVibrations;
using PlayFab;
using PlayFab.ClientModels;
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
		/// Requests if this device is Linked
		/// </summary>
		bool IsDeviceLinked { get; }

		/// <summary>
		/// Are Sound Effects enabled?
		/// </summary>
		bool IsSfxEnabled { get; set; }

		/// <summary>
		/// Is Background Music enabled?
		/// </summary>
		bool IsBgmEnabled { get; set; }

		/// <summary>
		/// Is dialogue enabled?
		/// </summary>
		bool IsDialogueEnabled { get; set; }

		/// <summary>
		/// Is Haptic feedback on device enabled?
		/// </summary>
		bool IsHapticOn { get; set; }

		/// <summary>
		/// Requests the enable property for dynamic movement joystick
		/// </summary>
		bool UseDynamicJoystick { get; set; }
		
		/// <summary>
		/// Requests the current detail level of the game
		/// </summary>
		GraphicsConfig.DetailLevel CurrentDetailLevel { get; set; }
		
		/// <summary>
		/// Requests the current FPS mode
		/// </summary>
		bool UseHighFpsMode { get; set; }

		/// <summary>
		/// Requests the player's title display name (excluding appended numbers)
		/// </summary>
		string DisplayNameTrimmed { get; }

		/// <summary>
		/// Obtains the player unique id
		/// </summary>
		string PlayerId { get; }

		/// <summary>
		/// Requests the last region that player was connected to
		/// </summary>
		IObservableField<string> ConnectionRegion { get; }

		/// <summary>
		/// Requests current device Id
		/// </summary>
		IObservableField<string> DeviceID { get; }
		
		/// <summary>
		/// Requests current device Id
		/// </summary>
		IObservableField<string> LastLoginEmail { get; }
		
		/// <summary>
		/// Requests the player's title display name (including appended numbers)
		/// </summary>
		IObservableField<string> DisplayName { get; }
		
		/// <summary>
		/// Sets the resolution mode for the 3D rendering of the app
		/// </summary>
		void SetDetailLevel(GraphicsConfig.DetailLevel highRes);
		
		/// <summary>
		/// Sets the low/high FPS mode for the app
		/// </summary>
		void SetFpsMode(bool useHighFpsMode);

		/// <summary>
		/// Marks the date when the game was last time reviewed
		/// </summary>
		void MarkGameAsReviewed();
	}

	/// <inheritdoc />
	public interface IAppLogic : IAppDataProvider
	{
		public void Init();
	}

	/// <inheritdoc cref="IAppLogic"/>
	public class AppLogic : AbstractBaseLogic<AppData>, IAppLogic, IGameLogicInitializer
	{
		private readonly DateTime _defaultZeroTime = new(2020, 1, 1);
		private readonly IAudioFxService<AudioId> _audioFxService;

		/// <inheritdoc />
		public bool IsFirstSession => Data.IsFirstSession;

		/// <inheritdoc />
		public bool IsGameReviewed => Data.GameReviewDate > _defaultZeroTime;

		/// <inheritdoc />
		public bool IsDeviceLinked => string.IsNullOrWhiteSpace(DeviceID.Value);

		/// <inheritdoc />
		public bool IsSfxEnabled
		{
			get => Data.SfxEnabled;
			set
			{
				Data.SfxEnabled = value;
				_audioFxService.IsSfxMuted = !value;
			}
		}

		/// <inheritdoc />
		public bool IsBgmEnabled
		{
			get => Data.BgmEnabled;
			set
			{
				Data.BgmEnabled = value;
				_audioFxService.IsBgmMuted = !value;
			}
		}

		/// <inheritdoc />
		public bool IsDialogueEnabled
		{
			get => Data.DialogueEnabled;
			set
			{
				Data.DialogueEnabled = value;
				_audioFxService.IsDialogueMuted = !value;
			}
		}

		/// <inheritdoc />
		public bool IsHapticOn
		{
			get => Data.HapticEnabled;
			set
			{
				Data.HapticEnabled = value;
				MMVibrationManager.SetHapticsActive(value);
			}
		}
		
		/// <inheritdoc />
		public bool UseDynamicJoystick
		{
			get => Data.UseDynamicJoystick;
			set => Data.UseDynamicJoystick = value;
		}

		/// <inheritdoc />
		public GraphicsConfig.DetailLevel CurrentDetailLevel
		{
			get => Data.CurrentDetailLevel;
			set
			{
				Data.CurrentDetailLevel = value;
				SetDetailLevel(value);
			}
		}
		
		/// <inheritdoc />
		public bool UseHighFpsMode
		{
			get => Data.UseHighFpsMode;
			set
			{
				Data.UseHighFpsMode = value;
				SetFpsMode(value);
			}
		}

		/// <inheritdoc />
		public IObservableField<string> ConnectionRegion { get; private set; }

		/// <inheritdoc />
		public IObservableField<string> DeviceID { get; private set; }
		
		/// <inheritdoc />
		public IObservableField<string> LastLoginEmail { get; private set; }

		/// <inheritdoc />
		public IObservableField<string> DisplayName { get; private set; }

		/// <inheritdoc />
		public string DisplayNameTrimmed =>
			DisplayName == null || string.IsNullOrWhiteSpace(DisplayName.Value) || DisplayName.Value.Length < 5
				? "" : DisplayName.Value.Substring(0, DisplayName.Value.Length - 5);

		/// <inheritdoc />
		public string PlayerId => Data.PlayerId;

		public AppLogic(IGameLogic gameLogic, IDataProvider dataProvider, IAudioFxService<AudioId> audioFxService) :
			base(gameLogic, dataProvider)
		{
			_audioFxService = audioFxService;
		}

		/// <inheritdoc />
		public void Init()
		{
			IsSfxEnabled = Data.SfxEnabled;
			IsBgmEnabled = Data.BgmEnabled;
			IsDialogueEnabled = Data.DialogueEnabled;
			DisplayName = new ObservableResolverField<string>(() => Data.DisplayName, name => Data.DisplayName = name);
			ConnectionRegion = new ObservableResolverField<string>(() => Data.ConnectionRegion, region => Data.ConnectionRegion = region);
			DeviceID = new ObservableResolverField<string>(() => Data.DeviceId, linked => Data.DeviceId = linked);
			LastLoginEmail = new ObservableResolverField<string>(() => Data.LastLoginEmail, email => Data.LastLoginEmail = email);
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

		/// <inheritdoc />
		public void SetDetailLevel(GraphicsConfig.DetailLevel detailLevel)
		{
			var detailLevelConf = GameLogic.ConfigsProvider.GetConfig<GraphicsConfig>().DetailLevels
			                               .Find(detailLevelConf => detailLevelConf.Name == detailLevel);

			QualitySettings.SetQualityLevel(detailLevelConf.DetailLevelIndex);
		}
		
		/// <inheritdoc />
		public void SetFpsMode(bool useHighFpsMode)
		{
			var targetFps = useHighFpsMode
				                ? GameConstants.Visuals.HIGH_FPS_MODE_TARGET
				                : GameConstants.Visuals.LOW_FPS_MODE_TARGET;
			
			Application.targetFrameRate = targetFps;
		}
	}
}