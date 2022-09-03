using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Server.SDK.Modules.GameConfiguration;

namespace Backend.Game
{
	/// <summary>
	/// Server implemementation of ConfigsProvider.
	/// Mainly for more explicit checks and/or error messages.
	/// </summary>
	public class ServerConfigsProvider : ConfigsProvider
	{

		/// <summary>
		/// Obtains the config by a hard-typed type as opposed to a generic type
		/// </summary>
		public IEnumerable GetConfigByType(Type type)
		{
			GetAllConfigs().TryGetValue(type, out var cfg);
			return cfg;
		}
		
		public new IReadOnlyDictionary<int, T> GetConfigsDictionary<T>() 
		{
			if (GetAllConfigs().TryGetValue(typeof(T), out var cfg))
			{
				return cfg as IReadOnlyDictionary<int, T>;
			}
			throw new InvalidOperationException($"The Config {typeof(T)} was not found on the server. " +
			                                    $"Please ensure the server-side configs are up-to-date.");
		}
	}
	
}

