using System;
using System.Collections.Generic;
using FirstLight;

namespace Backend.Game
{
	/// <summary>
	/// Server implemementation of ConfigsProvider.
	/// Mainly for more explicit checks and/or error messages.
	/// </summary>
	public class ServerConfigsProvider : ConfigsProvider
	{
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

