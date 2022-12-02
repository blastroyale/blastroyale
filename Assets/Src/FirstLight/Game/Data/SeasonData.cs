using System;

namespace FirstLight.Game.Data
{
	[Serializable]
	public class SeasonData
	{
		public uint CurrentSeason = 1;
		
		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + CurrentSeason.GetHashCode();
			return hash;
		}
	}
}