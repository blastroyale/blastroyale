using System;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic.RPC;
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
		/// Resuests the current detail level of the game
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
		/// Requests the last region that player was connected to
		/// </summary>
		IObservableField<string> ConnectionRegion { get; }

		/// <summary>
		/// Requests current device Id
		/// </summary>
		IObservableField<string> DeviceId { get; }
		
		/// <summary>
		/// Requests current device Id
		/// </summary>
		IObservableField<string> LastLoginEmail { get; }
		
		/// <summary>
		/// Requests the player's Nickname
		/// </summary>
		IObservableFieldReader<string> NicknameId { get; }

		/// <summary>
		/// Sets the resolution mode for the 3D rendering of the app
		/// </summary>
		void SetDetailLevel(GraphicsConfig.DetailLevel highRes);

		/// <summary>
		/// Marks the date when the game was last time reviewed
		/// </summary>
		void MarkGameAsReviewed();

		/// <summary>
		/// Unlinks this device current account
		/// </summary>
		void LinkDeviceID(Action linkSuccessCallback, Action<PlayFabError> playfabErrorCallback);

		/// <summary>
		/// Unlinks this device current account
		/// </summary>
		void UnlinkDeviceID(Action unlinkSuccessCallback, Action<PlayFabError> playfabErrorCallback);
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
		public IObservableField<string> ConnectionRegion { get; private set; }
		
		/// <inheritdoc />
		public IObservableField<string> DeviceID { get; private set; }
		
		/// <inheritdoc />
		public IObservableField<string> LastLoginEmail { get; private set; }

		/// <inheritdoc />
		public string Nickname =>
			NicknameId == null || string.IsNullOrWhiteSpace(NicknameId.Value) || NicknameId.Value.Length < 5
				? ""
				: NicknameId.Value.Substring(0, NicknameId.Value.Length - 5);

		/// <inheritdoc />
		public string PlayerId => Data.PlayerId;

		/// <inheritdoc />
		public IObservableField<string> NicknameId { get; private set; }

		/// <inheritdoc />
		public IObservableField<string> SelectedGameModeId { get; private set; }

		/// <inheritdoc />
		IObservableFieldReader<string> IAppDataProvider.NicknameId => NicknameId;

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
			NicknameId = new ObservableResolverField<string>(() => Data.NickNameId, name => Data.NickNameId = name);
			ConnectionRegion = new ObservableResolverField<string>(() => Data.ConnectionRegion, region => Data.ConnectionRegion = region);
			DeviceID = new ObservableResolverField<string>(() => Data.DeviceId, linked => Data.DeviceId = linked);
			LastLoginEmail = new ObservableResolverField<string>(() => Data.LastLoginEmail, email => Data.LastLoginEmail = email);
			SelectedGameModeId = new ObservableField<string>(GameLogic.ConfigsProvider.GetConfigsList<QuantumGameModeConfig>()[0].Id);
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
		public void LinkDeviceID(Action linkSuccessCallback, Action<PlayFabError> playfabErrorCallback)
		{
#if UNITY_EDITOR
			var link = new LinkCustomIDRequest
			{
				CustomId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};

			PlayFabClientAPI.LinkCustomID(link, _ => OnLinkSuccess(), playfabErrorCallback);
#elif UNITY_ANDROID
			var link = new LinkAndroidDeviceIDRequest
			{
				AndroidDevice = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};
			
			PlayFabClientAPI.LinkAndroidDeviceID(link, _ => OnLinkSuccess(), playfabErrorCallback);

#elif UNITY_IOS
			var link = new LinkIOSDeviceIDRequest
			{
				DeviceModel = SystemInfo.deviceModel,
				OS = SystemInfo.operatingSystem,
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
				ForceLink = true
			};
			
			PlayFabClientAPI.LinkIOSDeviceID(link, _ => OnLinkSuccess(), playfabErrorCallback);
#endif
			void OnLinkSuccess()
			{
				linkSuccessCallback?.Invoke();
				_deviceId.Value = PlayFabSettings.DeviceUniqueIdentifier;
			}
		}

		/// <inheritdoc />
		public void UnlinkDeviceID(Action unlinkSuccessCallback, Action<PlayFabError> playfabErrorCallback)
		{
#if UNITY_EDITOR
			var unlinkRequest = new UnlinkCustomIDRequest
			{
				CustomId = PlayFabSettings.DeviceUniqueIdentifier
			};

			PlayFabClientAPI.UnlinkCustomID(unlinkRequest, OnUnlinkSuccess, playfabErrorCallback);

			void OnUnlinkSuccess(UnlinkCustomIDResult result)
			{
				unlinkSuccessCallback?.Invoke();
				_deviceId.Value = "";
			}
#elif UNITY_ANDROID
			var unlinkRequest = new UnlinkAndroidDeviceIDRequest
			{
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};
			
			PlayFabClientAPI.UnlinkAndroidDeviceID(unlinkRequest,OnUnlinkSuccess,OnUnlinkFail);
			
			void OnUnlinkSuccess(UnlinkAndroidDeviceIDResult result)
			{
				unlinkSuccessCallback?.Invoke();
				_deviceId.Value = "";
			}
#elif UNITY_IOS
			var unlinkRequest = new UnlinkIOSDeviceIDRequest
			{
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};

			PlayFabClientAPI.UnlinkIOSDeviceID(unlinkRequest, OnUnlinkSuccess, OnUnlinkFail);
			
			void OnUnlinkSuccess(UnlinkIOSDeviceIDResult result)
			{
				unlinkSuccessCallback?.Invoke();
				_deviceId.Value = "";
			}
#endif
		}
	}
}