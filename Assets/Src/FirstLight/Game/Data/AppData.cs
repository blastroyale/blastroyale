using System;
using System.Collections.Generic;
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
		
		public DateTime FirstLoginTime;
		public DateTime LastLoginTime;
		public DateTime LoginTime;
		public bool IsFirstSession;
		
		public string LastLoginEmail;
		public bool LinkedDevice;
		
		public DateTime GameReviewDate;

		public bool SfxEnabled = true;
		public bool BgmEnabled = true;
		public bool HapticEnabled = true;
		public DetailLevel CurrentDetailLevel;

		public List<UniqueId> NewUniqueIds = new List<UniqueId>();
		public List<GameId> GameIdsTagged = new List<GameId>();
		public List<UnlockSystem> SystemsTagged = new List<UnlockSystem>();

		public enum DetailLevel
		{
			Low, Medium, High
		}
	}


}