using FirstLight.Services;

namespace FirstLight.Game.Messages
{
	public struct ApplicationQuitMessage : IMessage { }
	public struct ApplicationPausedMessage : IMessage { public bool IsPaused; }
}