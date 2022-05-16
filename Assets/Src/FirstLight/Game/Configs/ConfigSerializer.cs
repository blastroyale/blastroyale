using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Utils;

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
		IConfigsProvider Deserialize(string serialized);
	}

	/// <summary>
	/// Class that represents the data to be serialized.
	/// </summary>
	[Serializable]
	internal class SerializedConfigs
	{
		public string Version;
		public List<PlayerLevelConfig> PlayerLevelConfigs;
	}

	/// <summary>
	/// Struct to represent what configs are serialized for the game.
	/// This configs are to be shared between client & server.
	/// </summary>
	public class ConfigsSerializer : IConfigsSerializer
	{
		public string Serialize(IConfigsProvider cfg, string version)
		{
			return ModelSerializer.Serialize(new SerializedConfigs()
			{
				Version = version,
				PlayerLevelConfigs = cfg.GetConfigsList<PlayerLevelConfig>()
			}).Value;
		}

		public IConfigsProvider Deserialize(string serialized)
		{
			var cfg = new ConfigsProvider();
			var configs = ModelSerializer.Deserialize<SerializedConfigs>(serialized);
			cfg.AddConfigs(cfg => (int)cfg.Level, configs.PlayerLevelConfigs);
			return cfg;
		}
	}
}