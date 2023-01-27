using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using Quantum;

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
	}
	
	/// <summary>
	/// Contains all the data in the scope of the Game's App
	/// </summary>
	[Serializable]
	public class AppData
	{
		public string DisplayName;
		public string PlayerId;
		public DateTime FirstLoginTime;
		public DateTime LastLoginTime;
		public DateTime LoginTime;
		public bool IsFirstSession;

		public bool UseDynamicJoystick = true;
		
		public string Environment;
		public string DeviceId;
		public string LastLoginEmail;
		public string ConnectionRegion;
		
		public DateTime GameReviewDate;

		public FrameSnapshot LastCapturedFrameSnapshot;

		public bool SfxEnabled = true;
		public bool BgmEnabled = true;
		public bool HapticEnabled = true;
		public bool DialogueEnabled = true;
		public int FpsTarget = 30;
		public GraphicsConfig.DetailLevel CurrentDetailLevel = GraphicsConfig.DetailLevel.Medium;
		public GameModeRotationConfig.GameModeEntry LastGameMode;
		public List<UnlockSystem> SystemsTagged = new ();
		public CustomGameOptions LastCustomGameOptions = new();
		
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
				DialogueEnabled = this.DialogueEnabled,
				ConnectionRegion = this.ConnectionRegion,
				FpsTarget = this.FpsTarget
			};
		}
	}


}