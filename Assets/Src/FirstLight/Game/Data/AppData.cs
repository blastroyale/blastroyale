using System;
using System.Collections.Generic;
using System.Numerics;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Data
{
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

		public bool SfxEnabled = true;
		public bool BgmEnabled = true;
		public bool HapticEnabled = true;
		public bool DialogueEnabled = true;
		public GraphicsConfig.DetailLevel CurrentDetailLevel = GraphicsConfig.DetailLevel.Medium;
		public bool UseHighFpsMode = false;
		
		public List<UniqueId> NewUniqueIds = new ();
		public List<GameId> GameIdsTagged = new ();
		public List<UnlockSystem> SystemsTagged = new ();
		
		public AppData Copy()
		{
			return new AppData
			{
				SfxEnabled = this.SfxEnabled,
				BgmEnabled = this.BgmEnabled,
				HapticEnabled = this.HapticEnabled,
				CurrentDetailLevel = this.CurrentDetailLevel
			};
		}
	}


}