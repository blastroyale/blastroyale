using System;
using System.Collections.Generic;
using System.ComponentModel;
using Backend.Game;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FirstLight.Game.Logic;


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

	/// <summary>
	/// Obtains a serialized model inside server's data.
	/// </summary>
	/// <typeparam name="T">Model type</typeparam>
	/// <returns>Returns an instance of the serialized model if present, else Null.</returns>
	public T GetModel<T>()
	{
		string data;
		return TryGetValue(typeof(T).Name, out data) ? ModelSerializer.Deserialize<T>(data) : default(T);
	}
}