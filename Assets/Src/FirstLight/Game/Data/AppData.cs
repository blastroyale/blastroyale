using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.MonoComponent.Ftue;
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
		
		public DateTime GameReviewDate;
		public IObservableField<int> MapId { get; private set; }
		
		public bool SfxEnabled = true;
		public bool BgmEnabled = true;
		public bool HapticEnabled = true;
		
		public List<UniqueId> NewUniqueIds = new List<UniqueId>();
		public List<GameId> GameIdsTagged = new List<GameId>();
		public List<UnlockSystem> SystemsTagged = new List<UnlockSystem>();

		public AppData()
		{
			MapId = new ObservableField<int>();
		}
	}
}