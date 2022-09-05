using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;

namespace FirstLight.Game.Messages
{
	public struct ApplicationQuitMessage : IMessage { }
	public struct ApplicationPausedMessage : IMessage { public bool IsPaused; }
	public struct ConfigurationUpdate : IMessage { public IConfigsProvider NewConfig; public IConfigsProvider OldConfig; }
}