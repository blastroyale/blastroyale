using FirstLight.Game.Services;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct MatchmakingJoinedMessage : IMessage
	{
		public JoinedMatchmaking Config;
	}

	/// <summary>
	/// This is not published if we found a game
	/// </summary>
	public struct MatchmakingLeftMessage : IMessage
	{
		public bool Error;
		public string Reason;
	}

	public struct MatchmakingMatchFoundMessage : IMessage
	{
		public GameMatched Game;
	}
}