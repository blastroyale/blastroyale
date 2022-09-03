
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FirstLight.Server.SDK.Modules.GameConfiguration
{

	/// <summary>
	/// Class that represents the data to be serialized.
	/// </summary>
	internal class SerializedConfigs
	{
		public string Version;
		
		public Dictionary<Type, IEnumerable> Configs;
	}

	/// <summary>
	/// This attribute can be added to config structs so they are ignored by the serializer.
	/// This way they won't be sent to server when they are not needed.
	/// </summary>
	[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
	public class IgnoreServerSerialization : Attribute { }

	/// <summary>
	/// Struct to represent what configs are serialized for the game.
	/// This configs are to be shared between client & server.
	/// </summary>
	public class ConfigsSerializer : IConfigsSerializer
	{
		private static JsonSerializerSettings settings = new JsonSerializerSettings()
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
			foreach (var type in configs.Keys)
			{
				if (type.CustomAttributes.Any(c => c.AttributeType == typeof(IgnoreServerSerialization)))
				{
					continue;
				}
				if (!type.IsSerializable)
				{
					throw new Exception(@$"Config {type} could not be serialized.
						 If this is not used in game logic please add [IgnoreServerSerialization]");
				}

				serializedConfig.Configs[type] = configs[type];
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
