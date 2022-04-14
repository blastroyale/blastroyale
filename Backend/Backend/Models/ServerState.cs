using System.Collections.Generic;
using FirstLight.Game.Utils;

namespace Backend.Models;


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

	public void SetModel(object model)
	{
		var (typeName, data) = ModelSerializer.Serialize(model);
		this[typeName] = data;
	}

	/// <summary>
	/// Obtains a serialized model inside server's data.
	/// </summary>
	public T DeserializeModel<T>()
	{
		return TryGetValue(typeof(T).FullName, out var data) ? ModelSerializer.Deserialize<T>(data) : default(T);
	}
}