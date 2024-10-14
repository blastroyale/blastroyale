using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FirstLight.Server.SDK.Modules
{
	/// <summary>
	/// Serializes models to Key Value pairs.
	/// Keys are defined as the model type name and value the model contents.
	/// </summary>
	public static class ModelSerializer
	{
		private static object _lock = new object();
		
		public static JsonSerializerSettings _settings = new JsonSerializerSettings()
		{
			NullValueHandling = NullValueHandling.Ignore,
			Converters = new List<JsonConverter>() { new StringEnumConverter() },
			DefaultValueHandling = DefaultValueHandling.Ignore
		};

		public static void RegisterConverter(JsonConverter converter)
		{
			if (_settings.Converters.All(c => c.GetType() != converter.GetType()))
			{
				lock (_lock)
				{
					if (_settings.Converters.All(c => c.GetType() != converter.GetType()))
					{
						_settings.Converters.Add(converter);
					}
				}
			}
		}

		/// <summary>
		/// Serializes a given object to a indented json string
		/// </summary>
		public static string PrettySerialize(object model)
		{
			var value = JsonConvert.SerializeObject(model, Formatting.Indented, _settings);
			return value;
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
		public static object Deserialize(Type type, string modelData)
		{
			return JsonConvert.DeserializeObject(modelData, type, _settings)!;
		}

		/// <summary>
		/// Generic wrapper of <see cref="Deserialize"/>
		/// </summary>
		public static TModel Deserialize<TModel>(string modelData)
		{
			return (TModel) Deserialize(typeof(TModel), modelData);
		}

		/// <summary>
		/// Searches given data for serialized models. If the type given is found, deserializes it.
		/// </summary>
		public static object? DeserializeFromData(Type type, Dictionary<string, string> data, bool createIfNeeded = false)
		{
			if (!data.TryGetValue(type.FullName, out var modelData))
			{
				return createIfNeeded ? Activator.CreateInstance(type) : null;
			}

			return JsonConvert.DeserializeObject(modelData, type, _settings);
		}

		/// <summary>
		/// Generic wrapper of <see cref="DeserializeFromData"/>
		/// </summary>
		public static TModel DeserializeFromData<TModel>(Dictionary<string, string> data, bool createIfNeeded = false)
		{
			return (TModel) DeserializeFromData(typeof(TModel), data, createIfNeeded)!;
		}

		/// <summary>
		/// Serializes the given model and inser in the given dictionary.
		/// </summary>
		public static void SerializeToData(Dictionary<string, string> data, object model)
		{
			var kv = Serialize(model);
			data[kv.Key] = kv.Value;
		}

		/// <summary>
		/// Deserializes a given string as a given model.
		/// Uses a type definition as opposed to generic to avoid breaking type safety.
		/// </summary>
		public static CastType Deserialize<CastType>(string modelData, Type modelType)
		{
			return (CastType) JsonConvert.DeserializeObject(modelData, modelType, _settings);
		}
	}
}