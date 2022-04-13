using System.Collections.Generic;
using FirstLight.Game.Logic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Backend.Game;

/// <summary>
/// Serializes models to Key Value pairs.
/// </summary>
public static class ModelSerializer
{
	private static JsonConverter _formatter = new StringEnumConverter();
	
	/// <summary>
	/// Serializes a given object to a key value pair.
	/// Key is the model type name, value is a string representation of the model.
	/// </summary>
	/// <param name="model"></param>
	public static KeyValuePair<string, string> Serialize(object model)
	{
		var key = model.GetType().Name;
		var value = JsonConvert.SerializeObject(model, _formatter);
		return new KeyValuePair<string, string>(key, value);
	}

	/// <summary>
	/// Deserializes a given string as a given model.
	/// </summary>
	/// <param name="s"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static T Deserialize<T>(string s)
	{
		return JsonConvert.DeserializeObject<T>(s, _formatter);
	}
	
}