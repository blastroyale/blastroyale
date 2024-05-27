using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.SDK.Services;

namespace FirstLight.Game.Messages
{
	public struct ApplicationQuitMessage : IMessage
	{
	}

	public struct ApplicationPausedMessage : IMessage
	{
		public bool IsPaused;
	}

	public struct ApplicationFocusMessage : IMessage
	{
		public bool IsFocus;
	}

	public struct ConfigurationUpdate : IMessage
	{
		public IConfigsProvider NewConfig;
		public IConfigsProvider OldConfig;
	}

	public struct FeatureFlagsChanged : IMessage
	{
	}
}