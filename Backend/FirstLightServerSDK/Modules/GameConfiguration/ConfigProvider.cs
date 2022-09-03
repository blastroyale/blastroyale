using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FirstLight.Server.SDK.Modules.GameConfiguration
{
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
		
		/// <inheritdoc />
		public IReadOnlyDictionary<Type, IEnumerable> GetAllConfigs()
		{
			return new ReadOnlyDictionary<Type, IEnumerable>(_configs);
		}

		/// <inheritdoc />
		public void AddAllConfigs(IDictionary<Type, IEnumerable> configs)
		{
			foreach (var type in configs.Keys)
			{
				_configs[type] = configs[type];
			}
		}
	}
}