using System;
using System.Collections;
using System.Collections.Generic;

namespace FirstLight.Server.SDK.Modules.GameConfiguration
{
		/// <summary>
	/// Provides all the Game's config static data, including the game design data
	/// Has the imported data from the Universal Google Sheet file on the web
	/// </summary>
	public interface IConfigsProvider
	{
		/// <summary>
		///  Gets the current version of the current configuration
		/// </summary>
		ulong Version { get; }
		
		/// <summary>
		/// Requests the Config of <typeparamref name="T"/> type and with the given <paramref name="id"/>.
		/// Returns true if there is a <paramref name="config"/> of <typeparamref name="T"/> type and with the
		/// given <paramref name="id"/>,  false otherwise.
		/// </summary>
		bool TryGetConfig<T>(int id, out T config);
		
		/// <summary>
		/// Requests the single unique Config of <typeparamref name="T"/> type
		/// </summary>
		T GetConfig<T>();
		
		/// <summary>
		/// Requests the Config of <typeparamref name="T"/> type and with the given <paramref name="id"/>
		/// </summary>
		T GetConfig<T>(int id);

		/// <summary>
		/// Requests the Config List of <typeparamref name="T"/> type
		/// </summary>
		List<T> GetConfigsList<T>();

		/// <summary>
		/// Requests the Config Dictionary of <typeparamref name="T"/> type
		/// </summary>
		IReadOnlyDictionary<int, T> GetConfigsDictionary<T>();

		/// <summary>
		/// Obtains all configs as a readonly dictionary
		/// </summary>
		public IReadOnlyDictionary<Type, IEnumerable> GetAllConfigs();
	}
}