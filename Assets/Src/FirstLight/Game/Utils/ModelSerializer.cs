using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PlayFab.ClientModels;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Serializes models to Key Value pairs.
	/// Keys are defined as the model type name and value the model contents.
	/// TODO: Move to Server SDK
	/// </summary>
	public static class ModelSerializer
	{
		private static JsonSerializerSettings _settings = new JsonSerializerSettings()
		{
			NullValueHandling = NullValueHandling.Ignore,
			Converters = new List<JsonConverter>()
			{
				new StringEnumConverter()
			}
		};

		public static void RegisterConverter(JsonConverter converter)
		{
			_settings.Converters.Add(converter);
		}
		
		/// <summary>
		/// Serializes a given object to a key value pair.
		/// Key is the model type name, value is a string representation of the model.
		/// </summary>
		public static KeyValuePair<string, string> Serialize(object model)
		{
			var key = model.GetType().FullName;
			var value = JsonConvert.SerializeObject(model, _settings);
			return new KeyValuePair<string, string>(key, value);
		}

		/// <summary>
		/// Deserializes a given string as a given model.
		/// </summary>
		public static TModel Deserialize<TModel>(string modelData)
		{
			return JsonConvert.DeserializeObject<TModel>(modelData, _settings);
		}
		
		/// <summary>
		/// Searches given data for serialized models. If the type given is found, deserializes it.
		/// </summary>
		public static TModel DeserializeFromData<TModel>(Dictionary<string, string> data, bool createIfNeeded = false)
		{
			if(!data.TryGetValue(typeof(TModel).FullName, out var modelData))
			{
				return createIfNeeded ? (TModel) Activator.CreateInstance(typeof(TModel)) : default(TModel);
			}
			return JsonConvert.DeserializeObject<TModel>(modelData, _settings);
		}

		/// <summary>
		/// Serializes the given model and inser in the given dictionary.
		/// </summary>
		public static void SerializeToData(Dictionary<string, string> data, object model)
		{
			var (key, value) = Serialize(model);
			data[key] = value;
		}
		
		/// <summary>
		/// Searches given data for serialized models. If the type given is found, deserializes it.
		/// </summary>
		public static TModel DeserializeFromClientData<TModel>(Dictionary<string, UserDataRecord> data, bool createIfNeeded = false)
		{
			if(!data.TryGetValue(typeof(TModel).FullName, out var modelData))
			{
				return createIfNeeded ? (TModel) Activator.CreateInstance(typeof(TModel)) : default(TModel);
			}
			return JsonConvert.DeserializeObject<TModel>(modelData.Value, _settings);
		}
	
		/// <summary>
		/// Deserializes a given string as a given model.
		/// Uses a type definition as opposed to generic to avoid breaking type safety.
		/// </summary>
		public static CastType Deserialize<CastType>(string modelData, Type modelType)
		{
			return (CastType)JsonConvert.DeserializeObject(modelData, modelType, _settings);
		}
	
	}
}

