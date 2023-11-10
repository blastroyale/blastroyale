using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using Quantum;
using Environment = FirstLight.Game.Services.Environment;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Represents user custom game preferences, set in last custom game played.
	/// </summary>
	[Serializable]
	public class CustomGameOptions
	{
		public List<string> Mutators = new();
		public int GameModeIndex;
		public int MapIndex;
		public int BotDifficulty;
		public string WeaponLimiter;
	}
	
	/// <summary>
	/// The FPS values we support.
	/// </summary>
	public enum FpsTarget
	{
		Normal = 30,
		High = 60,
		Unlimited = 0,
	}
	
	/// <summary>
	/// Contains all the data in the scope of the Game's App
	/// </summary>
	[Serializable]
	public class AppData
	{
		public string DisplayName;
		public string PlayerId;
		public string AvatarUrl;
		public DateTime FirstLoginTime;
		public DateTime LastLoginTime;
		public DateTime LoginTime;
		public bool IsFirstSession;

		public bool UseDynamicJoystick = false;
		public bool UseDynamicCamera = true;
		public bool UseScreenShake = true;
		public bool InvertSpecialCancellling = true;

		public Environment LastEnvironment;
		public string DeviceId;
		public string LastLoginEmail;
		public string ConnectionRegion;

		public DateTime GameReviewDate;
		
		public FrameSnapshot LastCapturedFrameSnapshot;

		public bool ShowRealDamage = false;
		public bool SfxEnabled = true;
		public bool BgmEnabled = true;
		public bool HapticEnabled = true;
		
		public bool DialogueEnabled = true;
		public FpsTarget FpsTarget = FpsTarget.High;

		public GraphicsConfig.DetailLevel CurrentDetailLevel
		{
			get => GraphicsConfig.DetailLevel.Low;
			set { }
		}
		public GameModeRotationConfig.GameModeEntry LastGameMode;
		public CustomGameOptions LastCustomGameOptions = new();
		public bool ConeAim;
		public bool MovespeedControl;
		public bool AngleTapShoot;
		public bool StopShootingShake;
		
		[NonSerialized] public Dictionary<string, string> TitleData;
		
		/// <summary>
		/// Copies base values for when user logs in to a new environment.
		/// We want to maintain a few settings across environments, those settings
		/// should be added to this copy method.
		/// </summary>
		public AppData CopyForNewEnvironment()
		{
			return new AppData
			{
				SfxEnabled = this.SfxEnabled,
				BgmEnabled = this.BgmEnabled,
				HapticEnabled = this.HapticEnabled,
				CurrentDetailLevel = this.CurrentDetailLevel,
				UseDynamicJoystick = this.UseDynamicJoystick,
				UseDynamicCamera = this.UseDynamicCamera,
				DialogueEnabled = this.DialogueEnabled,
				ConnectionRegion = this.ConnectionRegion,
				FpsTarget = this.FpsTarget
			};
		}
	}


}