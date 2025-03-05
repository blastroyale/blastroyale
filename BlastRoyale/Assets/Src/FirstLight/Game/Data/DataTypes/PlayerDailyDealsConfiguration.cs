using System;
using System.Collections.Generic;

namespace FirstLight.Game.Data.DataTypes
{
	public class PlayerDailyDealsConfiguration
	{
		public DateTime ResetDealsAt { get; set; }

		public List<PlayerSpecialStoreData> SpecialStoreList = new ();
		
		public override int GetHashCode()
		{
			int hash = 17;
			
			foreach (var e in SpecialStoreList)
				hash = hash * 23 + e.GetHashCode();
			
			return hash;
		}
	}
}