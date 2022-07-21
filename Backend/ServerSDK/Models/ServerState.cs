using System;
using ServerSDK.Modules;

namespace ServerSDK.Models;


/// <summary>
/// Represents stored server data.
/// Stores { type names : serialized type } in its internal data. 
/// </summary>
public class ServerState : Dictionary<string, string>
{
	public ServerState() 
	{
	}

	public ServerState(Dictionary<string, string> data): base(data)
	{
	}

	public ulong GetVersion()
	{
		if (!TryGetValue("version", out var versionString))
		{
			versionString = "1";
		}
		return ulong.Parse(versionString);
	}

	public void SetVersion(ulong version)
	{
		this["version"] = version.ToString();
	}

	/// <summary>
	/// Sets a given model in server state.
	/// Will serialize the model.
	/// </summary>
	public void SetModel(object model)
	{
		var (typeName, data) = ObjectSerializer.Serialize(model);
		this[typeName] = data;
	}

	/// <summary>
	/// Obtains a serialized model inside server's data.
	/// </summary>
	public T DeserializeModel<T>()
	{
		return TryGetValue(typeof(T).FullName, out var data)
			       ? ObjectSerializer.Deserialize<T>(data)
			       : Activator.CreateInstance<T>();
	}
}