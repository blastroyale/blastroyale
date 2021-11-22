using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLight
{
	/// <summary>
	/// Provides all the Game's config static data, including the game design data
	/// Has the imported data from the Universal Google Sheet file on the web
	/// </summary>
	public interface IConfigsProvider
	{
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
	}

	/// <inheritdoc />
	/// <remarks>
	/// Extends the <see cref="IConfigsProvider"/> behaviour by allowing it to add configs to the provider
	/// </remarks>
	public interface IConfigsAdder : IConfigsProvider
	{
		/// <summary>
		/// Adds the given unique single <paramref name="config"/> to the container.
		/// </summary>
		void AddSingletonConfig<T>(T config);

		/// <summary>
		/// Adds the given <paramref name="configList"/> to the container.
		/// The configuration will use the given <paramref name="referenceIdResolver"/> to map each config to it's defined id.
		/// </summary>
		void AddConfigs<T>(Func<T, int> referenceIdResolver, IList<T> configList) where T : struct;
	}
	
	/// <inheritdoc />
	public class ConfigsProvider : IConfigsAdder
	{
		private const int _singleConfigId = 0;
		
		private readonly IDictionary<Type, IEnumerable> _configs = new Dictionary<Type, IEnumerable>();

		/// <inheritdoc />
		public bool TryGetConfig<T>(int id, out T config)
		{
			return GetConfigsDictionary<T>().TryGetValue(id, out config);
		}

		/// <inheritdoc />
		public T GetConfig<T>()
		{
			var dictionary = GetConfigsDictionary<T>();

			if (!dictionary.TryGetValue(_singleConfigId, out var config))
			{
				throw new InvalidOperationException($"The Config container for {typeof(T)} is not a single config container. " +
				                                    $"Use either 'GetConfig<T>(int id)' or 'GetConfigsList<T>()' to get your needed config");
			}
			
			return config;
		}

		/// <inheritdoc />
		public T GetConfig<T>(int id)
		{
			return GetConfigsDictionary<T>()[id];
		}

		/// <inheritdoc />
		public List<T> GetConfigsList<T>()
		{
			return new List<T>(GetConfigsDictionary<T>().Values);
		}

		/// <inheritdoc />
		public IReadOnlyDictionary<int, T> GetConfigsDictionary<T>() 
		{
			return _configs[typeof(T)] as IReadOnlyDictionary<int, T>;
		}

		/// <inheritdoc />
		public void AddSingletonConfig<T>(T config)
		{
			_configs.Add(typeof(T), new Dictionary<int, T> {{ _singleConfigId, config }});
		}

		/// <inheritdoc />
		public void AddConfigs<T>(Func<T, int> referenceIdResolver, IList<T> configList) where T : struct
		{
			var dictionary = new Dictionary<int, T>(configList.Count);

			for (int i = 0; i < configList.Count; i++)
			{
				dictionary.Add(referenceIdResolver(configList[i]), configList[i]);
			}

			_configs.Add(typeof(T), dictionary);
		}
	}
}