using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Modules;
using Newtonsoft.Json;
using Photon.Deterministic;

namespace FirstLight.Game.Serializers
{
	[Serializable]
	internal struct SerializableVector
	{
		public FP X;
		public FP Y;
		public FP Z;

		public bool IsZero()
		{
			return X == 0 && Y == 0 && Z == 0;
		}

		public static SerializableVector From(FPVector3 v)
		{
			return new SerializableVector()
			{
				X = v.X,
				Y = v.Y,
				Z = v.Z
			};
		}

		public static SerializableVector From(FPVector2 v)
		{
			return new SerializableVector()
			{
				X = v.X,
				Y = v.Y,
			};
		}
	}

	/// <summary>
	/// Vector3 newtonsoft converter
	/// </summary>
	public class QuantumVector3Converter : JsonConverter<FPVector3>
	{
		public override void WriteJson(JsonWriter writer, FPVector3 v, JsonSerializer serializer)
		{
			serializer.Serialize(writer, SerializableVector.From(v));
		}

		public override FPVector3 ReadJson(JsonReader reader, Type objectType, FPVector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var values = serializer.Deserialize<SerializableVector>(reader);
			if (values.IsZero())
				return FPVector3.Zero;
			return new FPVector3(values.X, values.Y, values.Z);
		}
	}

	/// <summary>
	/// Vector2 newtonsoft converter
	/// </summary>
	public class QuantumVector2Converter : JsonConverter<FPVector2>
	{
		public override void WriteJson(JsonWriter writer, FPVector2 v, JsonSerializer serializer)
		{
			serializer.Serialize(writer, SerializableVector.From(v));
		}

		public override FPVector2 ReadJson(JsonReader reader, Type objectType, FPVector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var values = serializer.Deserialize<SerializableVector>(reader);
			if (values.IsZero())
				return FPVector2.Zero;
			return new FPVector2(values.X, values.Y);
		}
	}

	[Serializable]
	internal struct SerializableFP
	{
		public long RawValue;

		public static SerializableFP From(FP fp)
		{
			return new SerializableFP()
			{
				RawValue = fp.RawValue
			};
		}

		public FP ToFP()
		{
			return FP.FromRaw(RawValue);
		}
	}

	/// <summary>
	/// JSON serializer for FP's
	/// </summary>
	public class FPConverter : JsonConverter<FP>
	{
		public override void WriteJson(JsonWriter writer, FP v, JsonSerializer serializer)
		{
			writer.WriteValue(v.ToString());
		}

		public override FP ReadJson(
			JsonReader reader,
			Type objectType,
			FP existingValue,
			bool hasExistingValue,
			JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.StartObject)
			{
				SerializableFP serializableVector = serializer.Deserialize<SerializableFP>(reader);
				return serializableVector.ToFP();
			}

			if (reader.TokenType == JsonToken.String)
			{
				return FP.FromString((string)reader.Value);
			}

			throw new Exception("Unable to parse FP from json!");
		}
	}

	public class CustomDictionaryConverter<TKey, TValue> : JsonConverter
	{
		public CustomDictionaryConverter()
		{
		}

		public override bool CanConvert(Type objectType) => objectType == typeof(Dictionary<TKey, TValue>);

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => serializer.Serialize(writer, ((Dictionary<TKey, TValue>) value).ToList());

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => serializer.Deserialize<KeyValuePair<TKey, TValue>[]>(reader).ToDictionary(kv => kv.Key, kv => kv.Value);
	}

	/// <summary>
	/// Manipulates the custom serializers implemented
	/// </summary>
	public static class FLGCustomSerializers
	{
		/// <summary>
		/// Registers specific type serializers that are shared between the client and server.
		/// </summary>
		public static void RegisterSerializers()
		{
			ModelSerializer.RegisterConverter(new QuantumVector2Converter());
			ModelSerializer.RegisterConverter(new QuantumVector3Converter());
			ModelSerializer.RegisterConverter(new FPConverter());
			ModelSerializer.RegisterConverter(new EquipmentSerializer());
			ModelSerializer.RegisterConverter(new LocalizableStringSerializer());
			ModelSerializer.RegisterConverter(new DurationSerializer());
		}

		public static void RegisterAOT()
		{
			// TODO Move this to AOTCode and reformat this file
			Newtonsoft.Json.Utilities.AotHelper.EnsureList<CollectionTrait>();
		}
	}
}