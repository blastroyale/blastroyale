

using FirstLight.SDK.Services;

namespace FirstLight.Server.SDK.Models
{
	/// <summary>
	/// Wrapper of a game logic message. It wraps the message mainly to add
	/// the message user, as the message itself is user agnostic but when implementing
	/// listeners on server we need to know which user triggered the message.
	/// </summary>
	public class GameLogicMessageEvent<TMsg> : GameServerEvent where TMsg : IMessage
	{
		public string UserId { get; }
		public TMsg Message { get; }
		
		public GameLogicMessageEvent(string user, TMsg message)
		{
			Message = message;
			UserId = user;
		}
	}
}