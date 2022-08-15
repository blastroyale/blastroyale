using System;
using Newtonsoft.Json;
using Photon.Deterministic;

namespace FirstLight.Game.Utils
{
	[Serializable]
	internal class SerializableVector
	{
		public FP X;
		public FP Y;
		public FP Z;

		public bool IsZero()
		{
			return X == 0 && Y == 0 && Z == 0;
		}
		
		public static SerializableVector? From(FPVector3 v)
		{
			return new SerializableVector()
			{
				X = v.X,
				Y = v.Y,
				Z = v.Z
			};
		}
		
		public static SerializableVector? From(FPVector2 v)
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
			if (values == null || values.IsZero())
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
			if (values == null || values.IsZero())
				return FPVector2.Zero;
			return new FPVector2(values.X, values.Y);
		}
	}
}
