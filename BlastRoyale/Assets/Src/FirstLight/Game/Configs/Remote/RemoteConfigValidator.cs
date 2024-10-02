using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs.Remote.FirstLight.Game.Configs.Remote;
using FirstLightServerSDK.Services;

namespace FirstLight.Game.Configs.Remote
{
	public class ConfigParseException : Exception
	{
		public ConfigParseException(string message) : base(message)
		{
		}
	}

	public static class RemoteConfigValidator
	{
		public static Type[] Configs = new[]
		{
			typeof(EventGameModesConfig),
			typeof(EventNotificationConfig),
			typeof(FixedGameModesConfig),
			typeof(LeaderboardSeasons),
			typeof(MatchmakingQueuesConfig)
		};

		public static void ValidateConfigs(IRemoteConfigProvider configProvider)
		{
			List<Type> failed = new List<Type>();
			foreach (var config in Configs)
			{
				// Go trough all of them just to see all problems at the same time
				if (!configProvider.ValidateConfig(config))
				{
					failed.Add(config);
				}
			}

			if (failed.Count > 0)
			{
#if DEBUG
				throw new ConfigParseException("Failed to parse configs: \n" + string.Join("\n", failed.Select(a => a.Name)));
#else
				throw new ConfigParseException("Failed to parse configs!");
#endif
			}
		}
	}
}