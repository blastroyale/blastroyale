using System;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic.RPC;
using FirstLight.Services;
using MoreMountains.NiceVibrations;
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
		/// Is high res mode on device enabled?
		/// </summary>
		GraphicsConfig.DetailLevel CurrentDetailLevel { get; set; }

		/// <summary>
		/// Requests the player's Nickname
		/// </summary>
		string Nickname { get; }

		/// <summary>
		/// Obtains the player unique id
		/// </summary>
		string PlayerId { get; }
		
		/// <summary>
		/// Requests the player's Nickname
		/// </summary>
		IObservableFieldReader<string> NicknameId { get; }
		
		/// <summary>
		/// Requests current device Id
		/// </summary>
		IObservableFieldReader<string> DeviceId { get; }

		/// <summary>
		/// Requests current selected game mode
		/// Marks the date when the game was last time reviewed
		/// </summary>
		IObservableField<GameMode> SelectedGameMode { get; }

		/// <summary>
		/// Sets the resolution mode for the 3D rendering of the app
		/// </summary>
		void SetDetailLevel(GraphicsConfig.DetailLevel highRes);
		
		/// <summary>
		/// Marks the date when the game was last time reviewed
		/// </summary>
		void MarkGameAsReviewed();
	}

	/// <inheritdoc />
	public interface IAppLogic : IAppDataProvider
	{
		/// <summary>
		/// Requests and sets player nickname
		/// </summary>
		new IObservableField<string> NicknameId { get; }

		/// <summary>
		/// Unlinks this device current account
		/// </summary>
		void UnlinkDevice();
	}

	/// <inheritdoc cref="IAppLogic"/>
	public class AppLogic : AbstractBaseLogic<AppData>, IAppLogic, IGameLogicInitializer
	{
		private readonly DateTime _defaultZeroTime = new (2020, 1, 1);
		private readonly IAudioFxService<AudioId> _audioFxService;
		
		private IObservableField<string> _deviceId;

		/// <inheritdoc />
		public bool IsFirstSession => Data.IsFirstSession;

		/// <inheritdoc />
		public bool IsGameReviewed => Data.GameReviewDate > _defaultZeroTime;

		/// <inheritdoc />
		public bool IsDeviceLinked => string.IsNullOrWhiteSpace(_deviceId.Value);

		/// <inheritdoc />
		public bool IsSfxOn
		{
			get => Data.SfxEnabled;
			set
			{
				Data.SfxEnabled = value;
				_audioFxService.IsSfxMuted = !value;
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
			set
			{
				Data.HapticEnabled = value;
				MMVibrationManager.SetHapticsActive(value);
			}
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
		public string Nickname => NicknameId == null || string.IsNullOrWhiteSpace(NicknameId.Value) || NicknameId.Value.Length < 5 ?
			"" : NicknameId.Value.Substring(0, NicknameId.Value.Length - 5);

		/// <inheritdoc />
		public string PlayerId => Data.PlayerId;

		/// <inheritdoc />
		public IObservableField<string> NicknameId { get; private set; }

		/// <inheritdoc />
		public IObservableField<GameMode> SelectedGameMode { get; private set; }

		/// <inheritdoc />
		IObservableFieldReader<string> IAppDataProvider.NicknameId => NicknameId;
		/// <inheritdoc />
		IObservableFieldReader<string> IAppDataProvider.DeviceId => _deviceId;

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
			NicknameId = new ObservableResolverField<string>(() => Data.NickNameId, name => Data.NickNameId = name);
			_deviceId = new ObservableResolverField<string>(() => Data.DeviceId, linked => Data.DeviceId = linked);
			SelectedGameMode = new ObservableField<GameMode>(GameMode.BattleRoyale);
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
			Application.targetFrameRate = detailLevelConf.Fps;
		}

		/// <inheritdoc />
		public void UnlinkDevice()
		{
			_deviceId.Value = "";
		}
	}
}