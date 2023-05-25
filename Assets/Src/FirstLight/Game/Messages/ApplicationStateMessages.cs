using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;

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

	public struct EnvironmentChanged : IMessage
	{
		public Environment NewEnvironment;
	}
		public struct FeatureFlagsChanged : IMessage { }

}