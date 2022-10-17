using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Quantum
{
	/// <summary>
	/// Json serializer for Quantum Assets
	/// </summary>
	public class QuantumAssetSerializer : Quantum.JsonAssetSerializerBase
	{
		private readonly JsonSerializer _serializer = CreateSerializer();

		public static JsonSerializer CreateSerializer()
		{
			return JsonSerializer.Create(CreateSettings());
		}

		protected override object FromJson(string json, Type type)
		{
			using (var reader = new StringReader(json))
			{
				var result = _serializer.Deserialize(reader, type);
				return result;
			}
		}

		protected override string ToJson(object obj)
		{
			using (var writer = new StringWriter())
			{
				_serializer.Serialize(writer, obj);
				return writer.ToString();
			}
		}

		private static JsonSerializerSettings CreateSettings()
		{
			return new JsonSerializerSettings
			{
				ContractResolver = new WritablePropertiesOnlyResolver(),
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
				Converters = { new ByteArrayConverter(), new FixedSizeBufferConverter() },
			};
		}

		private class FixedSizeBufferConverter : JsonConverter
		{
			public override bool CanConvert(Type objectType)
			{
				if (!objectType.IsValueType)
				{
					return false;
				}

				if (!objectType.Name.EndsWith("e__FixedBuffer"))
				{
					return false;
				}

				if (objectType.GetAttribute<CompilerGeneratedAttribute>() != null &&
					objectType.GetAttribute<UnsafeValueTypeAttribute>() != null)
				{
					return true;
				}

				return false;
			}

			private Type GetFixedBufferElementType(Type fixedBufferType, out int totalSize)
			{
				if (fixedBufferType == null)
				{
					throw new ArgumentNullException(nameof(fixedBufferType));
				}

				if (!fixedBufferType.IsLayoutSequential)
				{
					throw new ArgumentException($"Type {fixedBufferType.FullName} is expected to have sequential layout", nameof(fixedBufferType));
				}

				var field = fixedBufferType.GetField("FixedElementField");
				if (field == null)
				{
					throw new ArgumentException($"Type {fixedBufferType.FullName} does not have FixedElementField field", nameof(fixedBufferType));
				}
				totalSize = fixedBufferType.StructLayoutAttribute.Size;
				return field.FieldType;
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{

				var fixedBufferElementType = GetFixedBufferElementType(objectType, out var totalSize);

				var result = Activator.CreateInstance(objectType);
				var handle = GCHandle.Alloc(result, GCHandleType.Pinned);

				try
				{
					var ptr = handle.AddrOfPinnedObject();

					if (fixedBufferElementType == typeof(byte)) ReadJsonArray<byte>(reader, ptr, totalSize);
					else if (fixedBufferElementType == typeof(sbyte)) ReadJsonArray<sbyte>(reader, ptr, totalSize);
					else if (fixedBufferElementType == typeof(short)) ReadJsonArray<short>(reader, ptr, totalSize);
					else if (fixedBufferElementType == typeof(ushort)) ReadJsonArray<ushort>(reader, ptr, totalSize);
					else if (fixedBufferElementType == typeof(int)) ReadJsonArray<int>(reader, ptr, totalSize);
					else if (fixedBufferElementType == typeof(uint)) ReadJsonArray<uint>(reader, ptr, totalSize);
					else if (fixedBufferElementType == typeof(long)) ReadJsonArray<long>(reader, ptr, totalSize);
					else if (fixedBufferElementType == typeof(ulong)) ReadJsonArray<ulong>(reader, ptr, totalSize);
					else if (fixedBufferElementType == typeof(bool)) ReadJsonArray<bool>(reader, ptr, totalSize);
					else if (fixedBufferElementType == typeof(char)) ReadJsonArray<char>(reader, ptr, totalSize);
					else
					{
						throw new NotSupportedException($"Type not supported: {fixedBufferElementType.FullName}");
					}

				}
				finally
				{
					handle.Free();
				}

				return result;
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{

				var fixedBufferElementType = GetFixedBufferElementType(value.GetType(), out var totalSize);

				var handle = GCHandle.Alloc(value, GCHandleType.Pinned);

				try
				{
					var ptr = handle.AddrOfPinnedObject();
					if (fixedBufferElementType == typeof(byte)) WriteJsonArray<byte>(writer, ptr, totalSize);
					else if (fixedBufferElementType == typeof(sbyte)) WriteJsonArray<sbyte>(writer, ptr, totalSize);
					else if (fixedBufferElementType == typeof(short)) WriteJsonArray<short>(writer, ptr, totalSize);
					else if (fixedBufferElementType == typeof(ushort)) WriteJsonArray<ushort>(writer, ptr, totalSize);
					else if (fixedBufferElementType == typeof(int)) WriteJsonArray<int>(writer, ptr, totalSize);
					else if (fixedBufferElementType == typeof(uint)) WriteJsonArray<uint>(writer, ptr, totalSize);
					else if (fixedBufferElementType == typeof(long)) WriteJsonArray<long>(writer, ptr, totalSize);
					else if (fixedBufferElementType == typeof(ulong)) WriteJsonArray<ulong>(writer, ptr, totalSize);
					else if (fixedBufferElementType == typeof(bool)) WriteJsonArray<bool>(writer, ptr, totalSize);
					else if (fixedBufferElementType == typeof(char)) WriteJsonArray<char>(writer, ptr, totalSize);
					else
					{
						throw new NotSupportedException($"Type not supported: {fixedBufferElementType.FullName}");
					}
				}
				finally
				{
					handle.Free();
				}
			}

			private unsafe void WriteJsonArray<T>(JsonWriter writer, IntPtr ptr, int totalSize) where T : unmanaged
			{

				writer.WriteStartArray();

				var count = totalSize / sizeof(T);
				Assert.Check((totalSize % sizeof(T)) == 0);

				T* p = (T*)ptr;
				if (typeof(T) == typeof(char))
				{
					for (var i = 0; i < count; i++)
					{
						writer.WriteValue(Convert.ToInt32(p[i]));
					}
				}
				else
				{
					for (var i = 0; i < count; i++)
					{
						writer.WriteValue(p[i]);
					}
				}

				writer.WriteEndArray();
			}

			private unsafe void ReadJsonArray<T>(JsonReader reader, IntPtr ptr, int totalSize) where T : unmanaged
			{
				if (reader.TokenType != JsonToken.StartArray)
				{
					throw new Exception(string.Format("Unexpected token parsing fixed-size buffer. Expected StartArray, got {0}.", reader.TokenType));
				}

				var count = totalSize / sizeof(T);
				Assert.Check((totalSize % sizeof(T)) == 0);

				T* p = (T*)ptr;
				int i = 0;

				while (reader.Read())
				{
					switch (reader.TokenType)
					{
						case JsonToken.Boolean:
						case JsonToken.Integer:
							if (i >= count)
							{
								throw new Exception($"Fixed-size buffer exceeded");
							}
							p[i++] = (T)Convert.ChangeType(reader.Value, typeof(T));
							break;

						case JsonToken.EndArray:
							return;

						case JsonToken.Comment:
							// skip
							break;

						default:
							throw new Exception(string.Format("Unexpected token when reading fixed-size buffer: {0}", reader.TokenType));
					}
				}

				throw new Exception("Unexpected end when reading fixed-size buffer.");
			}
		}


		private class ByteArrayConverter : JsonConverter
		{

			public override bool CanConvert(Type objectType)
			{
				return objectType == typeof(byte[]);
			}

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				if (reader.TokenType == JsonToken.StartArray)
				{
					var byteList = new List<byte>();

					while (reader.Read())
					{
						switch (reader.TokenType)
						{
							case JsonToken.Integer:
								byteList.Add(Convert.ToByte(reader.Value));
								break;

							case JsonToken.EndArray:
								return byteList.ToArray();

							case JsonToken.Comment:
								// skip
								break;

							default:
								throw new Exception(string.Format("Unexpected token when reading bytes: {0}", reader.TokenType));
						}
					}

					throw new Exception("Unexpected end when reading bytes.");
				}
				else
				{
					throw new Exception(string.Format("Unexpected token parsing binary. Expected StartArray, got {0}.", reader.TokenType));
				}
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				if (value == null)
				{
					writer.WriteNull();
					return;
				}

				byte[] data = (byte[])value;

				// compose an array
				writer.WriteStartArray();

				for (var i = 0; i < data.Length; i++)
				{
					writer.WriteValue(data[i]);
				}

				writer.WriteEndArray();
			}
		}

		private class WritablePropertiesOnlyResolver : DefaultContractResolver
		{

			protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
			{
				IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
				return props.Where(p => p.Writable).ToList();
			}

			protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
			{
				if (member is FieldInfo)
				{
					// just fields
					return base.CreateProperty(member, memberSerialization);
				}
				else
				{
					return null;
				}
			}
		}
	}
}