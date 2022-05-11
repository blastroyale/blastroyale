using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Ftue;
using FirstLight.Game.Utils;
using FirstLight.Game.Configs;
using FirstLight.Services;
using MoreMountains.NiceVibrations;
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
		/// Is high res mode on device enabled?
		/// </summary>
		bool IsHighResModeEnabled { get; set; }
		
		/// <summary>
		/// Marks the date when the game was last time reviewed
		/// </summary>
		void MarkGameAsReviewed();

		/// <summary>
		/// Requests the player's Nickname
		/// </summary>
		string Nickname { get; }
		
		/// <summary>
		/// Requests the player's Nickname
		/// </summary>
		IObservableFieldReader<string> NicknameId { get; }

		/// <summary>
		/// Sets the resolution mode for the 3D rendering of the app
		/// </summary>
		void SetResolutionMode(bool highRes);
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
			set
			{
				Data.HapticEnabled = value;
				MMVibrationManager.SetHapticsActive(value);
			}
		}

		/// <inheritdoc />
		public bool IsHighResModeEnabled
		{
			get => Data.HighResModeEnabled;
			set
			{
				Data.HighResModeEnabled = value;
				SetResolutionMode(value);
			}
		}

		/// <inheritdoc />
		public string Nickname => NicknameId == null || string.IsNullOrWhiteSpace(NicknameId.Value) || NicknameId.Value.Length < 5 ?
			"" : NicknameId.Value.Substring(0, NicknameId.Value.Length - 5);

		/// <inheritdoc />
		IObservableFieldReader<string> IAppDataProvider.NicknameId => NicknameId;

		/// <inheritdoc />
		public IObservableField<string> NicknameId { get; private set; }

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
		
		/// <inheritdoc />
		public void SetResolutionMode(bool highRes)
		{
			var resolution = highRes ? GameConstants.DYNAMIC_RES_HIGH : GameConstants.DYNAMIC_RES_LOW;

			ScalableBufferManager.ResizeBuffers(resolution,resolution);
		}
	}
}