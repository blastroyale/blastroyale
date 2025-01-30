using System;

namespace FirstLight.Server.SDK.Events
{
	/// <summary>
	/// Called when a leaderboard is finished
	/// </summary>
	[Serializable]
	public class LeaderboardFinished : GameServerEvent
	{
		public string Metric;
		
		public LeaderboardFinished(string playerId) : base(playerId)
		{
		}
	}
}

