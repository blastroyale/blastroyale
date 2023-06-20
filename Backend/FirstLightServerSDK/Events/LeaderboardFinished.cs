using System;
using FirstLight.Server.SDK.Models;

namespace FirstLight.Server.SDK.Events
{
	/// <summary>
	/// Called when a leaderboard is finished
	/// </summary>
	public class LeaderboardFinished : GameServerEvent
	{

		public LeaderboardFinished(string playerId) : base(playerId)
		{
		}
		
	}
}

