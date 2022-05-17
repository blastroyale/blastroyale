using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace FirstLight
{
	/// <summary>
	/// Instance of a game-specific serializable IConfigsProvider.
	/// </summary>
	public interface IConfigsSerializer
	{
		/// <summary>
		/// Serialized specific given config keys from the configuration into a string.
		/// </summary>
		string Serialize(IConfigsProvider cfg, string version);

		/// <summary>
		/// Deserializes a given string. Instantiate and returns a new IConfigsProvider containing those
		/// deserialized configs.
		/// </summary>
		T Deserialize<T>(string serialized) where T : IConfigsAdder;
	}

	/// <summary>
	/// Class that represents the data to be serialized.
	/// </summary>
	internal class SerializedConfigs
	{
		public string Version;
		
		public Dictionary<Type, IEnumerable> Configs;
	}

	/// <summary>
	/// Struct to represent what configs are serialized for the game.
	/// This configs are to be shared between client & server.
	/// </summary>
	public class ConfigsSerializer : IConfigsSerializer
	{
		private static JsonSerializerSettings settings = new ()
		{
			TypeNameHandling = TypeNameHandling.Auto,
			Converters = new List<JsonConverter>()
			{
				new StringEnumConverter(),
			}
		};

		/// <inheritdoc />
		public string Serialize(IConfigsProvider cfg, string version)
		{
			var configs = cfg.GetAllConfigs();
			var serializedConfig = new SerializedConfigs()
			{
				Version = version,
				Configs = new Dictionary<Type, IEnumerable>()
			};
			foreach (var (type, configList) in configs)
			{
				if (!type.IsSerializable || type.Namespace == "Quantum") // TODO: Allow quantum configs
				{
					continue;
				}
				serializedConfig.Configs[type] = configList;
			}
			return JsonConvert.SerializeObject(serializedConfig, settings);
		}

		/// <inheritdoc />
		public T Deserialize<T>(string serialized) where T : IConfigsAdder
		{
			var cfg = Activator.CreateInstance(typeof(T)) as IConfigsAdder;
			var configs = JsonConvert.DeserializeObject<SerializedConfigs>(serialized, settings);
			cfg.AddAllConfigs(configs?.Configs);
			return (T)cfg;
		}

	}
}