using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Backend;

/// <summary>
/// Represents the server configuration yml file.
/// All public fields of this class are serialized/deserialized to config.yml file
/// located at the root folder of the project.
/// </summary>
public class ServerConfiguration
{
	public string MinClientVersion { get; set; }
	
	/// <summary>
	/// OBtains singleton server configuration.
	/// </summary>
	public static ServerConfiguration GetConfig() => _cfg;
	
	private static ServerConfiguration _cfg { get; set; }

	public static void LoadConfiguration(string path)
	{
		var deserializer = new DeserializerBuilder()
		                   .WithNamingConvention(new CamelCaseNamingConvention())
		                   .Build();
		path = Path.Combine(path, "config.yml");
		_cfg = deserializer.Deserialize<ServerConfiguration>(File.OpenText(path));
	}
}