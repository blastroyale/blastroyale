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
		public string NickNameId;
		public string PlayerId;
		public DateTime FirstLoginTime;
		public DateTime LastLoginTime;
		public DateTime LoginTime;
		public bool IsFirstSession;
		
		public string DeviceId;
		
		public DateTime GameReviewDate;

		public bool SfxEnabled = true;
		public bool BgmEnabled = true;
		public bool HapticEnabled = true;
		public GraphicsConfig.DetailLevel CurrentDetailLevel;

		public List<UniqueId> NewUniqueIds = new ();
		public List<GameId> GameIdsTagged = new ();
		public List<UnlockSystem> SystemsTagged = new ();
	}


}