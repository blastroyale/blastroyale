using System;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Store the last matchmaking queue so the player can leave it when reopening the game
	/// </summary>
	[Serializable]
	public class MatchmakingData
	{
		public string LastQueue;
	}
}