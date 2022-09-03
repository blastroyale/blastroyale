namespace FirstLight.Server.SDK.Modules.GameConfiguration
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

}