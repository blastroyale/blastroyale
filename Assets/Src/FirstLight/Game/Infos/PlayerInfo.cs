using System.Collections.Generic;
using FirstLight.Game.Configs;
using Quantum;

namespace FirstLight.Game.Infos
{
	public struct PlayerInfo
	{
		public string Nickname;
		public uint Level;
		public uint Xp;
		public uint TotalCollectedXp;
		public uint MaxLevel;
		public GameId Skin;
		public GameId DeathMarker;
		public uint TotalTrophies;
		public List<UnlockSystem> CurrentUnlockedSystems;
		public PlayerLevelConfig Config;

		/// <summary>
		/// Checks if the player will or not level up with the given <paramref name="addXp"/>
		/// </summary>
		public bool WillLevelUp(uint addXp)
		{
			return Level <= MaxLevel && Xp + addXp >= Config.LevelUpXP;
		}
	}
}