using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using Quantum;

namespace FirstLight.Game.Infos
{
	public struct TrophyRoadRewardInfo
	{
		public uint Level;
		public uint XpNeeded;
		public IReadOnlyList<UnlockSystem> UnlockedSystems;
		public bool IsCollected;
		public bool IsReadyToCollect;
		
		/// <summary>
		/// Requests the Trophy road reward for this level
		/// </summary>
		public RewardData Reward;
	}
}