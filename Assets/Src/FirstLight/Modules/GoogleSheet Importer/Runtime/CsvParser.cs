using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace

namespace FirstLight.GoogleSheetImporter
{
	/// <summary>
	/// Helper class to parse CSV text
	/// </summary>
	public static class CsvParser
	{
		public const string IGNORE_COLUMN_CHAR = "$";
		public const string IGNORE_FIELD_CHAR = "#";
		public const string SUB_LIST_SUFFIX = "[]";

		public static readonly char[] PairSplitChars = {':', '<', '>', '=', '|'};
		public static readonly char[] ArraySplitChars = {',', '(', ')', '[', ']', '{', '}'};
		public static readonly string[] NewLineChars = {"\r\n", "\r", "\n"};

		/// <summary>
		/// Parses the entire <paramref name="csv"/> text.
		/// Each row is an element on the returned list
		/// Each column is an element on the returned dictionary. The dictionary key will be the CSV header
		/// </summary>
		public static List<Dictionary<string, string>> ConvertCsv(string csv)
		{
			var lines = csv.Split(new[] {"\r\n"}, StringSplitOptions.None);
			var list = new List<Dictionary<string, string>>(lines.Length - 1);
			var headlines = EnumerateCsvLine(lines[0]);

			for (var i = 1; i < lines.Length; i++)
			{
				var dictionary = new Dictionary<string, string>(headlines.Length);
				var values = EnumerateCsvLine(lines[i]);

				for (var j = 0; j < headlines.Length; j++)
				{
					if (headlines[j].StartsWith(IGNORE_COLUMN_CHAR))
					{
						continue;
					}

					dictionary.Add(headlines[j], values[j].Trim());
				}

				list.Add(dictionary);
			}

			return list;
		}

		/// <inheritdoc cref="DeserializeTo"/>
		public static T DeserializeTo<T>(Dictionary<string, string> data,
		                                 params Func<string, Type, object>[] deserializers)
		{
			return (T) DeserializeTo(typeof(T), data, deserializers);
		}

		/// <summary>
		/// Deserializes the given CSV <paramref name="data"/> cell values to an object of the given <paramref name="type"/> type
		/// </summary>
		/// <remarks>
		/// It provides extra custom <paramref name="deserializers"/> to specific parsing
		/// </remarks>
		public static object DeserializeTo(Type type, Dictionary<string, string> data,
		                                   params Func<string, Type, object>[] deserializers)
		{
			var ignoreType = typeof(ParseIgnoreAttribute);
			var instance = Activator.CreateInstance(type);

			foreach (var field in type.GetFields())
			{
				if (!data.ContainsKey(field.Name))
				{
					continue;
				}

				if (field.GetCustomAttributes(ignoreType, false).Length == 1)
				{
					continue;
				}

				field.SetValue(instance, DeserializeObject(data[field.Name], field.FieldType, deserializers));
			}

			return instance;
		}

		/// <summary>
		/// Deserializes the given <paramref name="data"/> to the given <paramref name="type"/> in a <see cref="object"/>
		/// format as a result to be set via reflection.
		/// </summary>
		/// <remarks>
		/// It provides extra custom <paramref name="deserializers"/> to specific parsing
		/// </remarks>
		public static object DeserializeObject(string data, Type type,
		                                       params Func<string, Type, object>[] deserializers)
		{
			var listType = typeof(IList);
			var dictionaryType = typeof(IDictionary);
			var unityDictionaryType = typeof(UnitySerializedDictionary<,>).GetGenericTypeDefinition();

			if (type.IsArray)
			{
				return ArrayParse(data, type.GetElementType(), deserializers);
			}

			if (listType.IsAssignableFrom(type))
			{
				return ArrayParse(data, type.GenericTypeArguments[0], deserializers);
			}

			if (type.BaseType != null && type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition()
			                                                                .IsAssignableFrom(unityDictionaryType))
			{
				var types = type.BaseType.GenericTypeArguments;
				return DictionaryParse(data, types[0], types[1], type, deserializers);
			}

			if (dictionaryType.IsAssignableFrom(type))
			{
				var types = type.GenericTypeArguments;
				var keyType = types[0];
				var valueType = types[1];
				return DictionaryParse(data, keyType, valueType,
				                       typeof(Dictionary<,>).MakeGenericType(keyType, valueType), deserializers);
			}

			return Parse(data, type, deserializers);
		}

		/// <summary>
		/// Deserializes a list of custom complex (non-primitive) types, with their own headers / definitions.
		/// </summary>
		public static object DeserializeSubList(List<Dictionary<string, string>> data, int startIndex, Type type,
		                                        string fieldName,
		                                        params Func<string, Type, object>[] deserializers)
		{
			var subType = type.GetGenericArguments()[0];
			var subData = GetSubListDictionary(data, startIndex);

			var listType = typeof(List<>).MakeGenericType(subType);
			var list = Activator.CreateInstance(listType);
			var addMethod = listType.GetMethod("Add")!;

			foreach (var dict in subData)
			{
				addMethod.Invoke(list, new[] {DeserializeTo(subType, dict, deserializers)});
			}

			return list;
		}
		
		/// <summary>
		/// Extracts the data dictionary of a sub list from the base deserialization data of an object.
		/// </summary>
		private static List<Dictionary<string, string>> GetSubListDictionary(List<Dictionary<string, string>> data, int startIndex)
		{
			var headerMap = new Dictionary<string, string>();

			foreach (var (key, value) in data[startIndex])
			{
				if (value.EndsWith(SUB_LIST_SUFFIX)) continue;

				headerMap.Add(key, value);
			}

			var objData = new List<Dictionary<string, string>>();
			for (int i = startIndex + 1; i < data.Count; i++)
			{
				var dataLine = data[i];

				if (dataLine["Key"] != IGNORE_FIELD_CHAR) break;

				var rowData = new Dictionary<string, string>();

				foreach (var (key, value) in dataLine)
				{
					if (headerMap.TryGetValue(key, out var header))
					{
						rowData.Add(header, value);
					}
				}

				objData.Add(rowData);
			}

			return objData;
		}

		/// <summary>
		/// Parses the given <paramref name="text"/> into a possible array of the given <typeparamref name="T"/>
		/// A text is in array format as long as is divided by ',', '{}', '()' or '[]' (ex: 1,2,3; {1,2}{4,5}, [1,2,3])
		/// If the given <paramref name="text"/> is not in an array format, it will return an array with <paramref name="text"/> as the only element
		/// </summary>
		/// <remarks>
		/// It provides extra custom <paramref name="deserializers"/> to specific parsing
		/// </remarks>
		/// <exception cref="FormatException">
		/// Thrown if the given <paramref name="text"/> is not in the given <typeparamref name="T"/> type format
		/// </exception>
		public static List<T> ArrayParse<T>(string text, params Func<string, Type, object>[] deserializers)
		{
			return ArrayParse(text, typeof(T), deserializers) as List<T>;
		}

		/// <inheritdoc cref="ArrayParse{T}" />
		public static object ArrayParse(string data, Type type, params Func<string, Type, object>[] deserializers)
		{
			var split = data.Split(ArraySplitChars);
			var list = (IList) Activator.CreateInstance(typeof(List<>).MakeGenericType(type));

			foreach (var value in split)
			{
				if (string.IsNullOrEmpty(value))
				{
					continue;
				}

				list.Add(Parse(value, type, deserializers));
			}

			return list;
		}

		/// <summary>
		/// Parses the given <paramref name="text"/> into a <seealso cref="Dictionary{TKey, TValue}"/> type.
		/// A text is in <seealso cref="Dictionary{TKey, TValue}"/> type format if follows the same rules
		/// of <seealso cref="ArrayParse{T}"/> and has at least 2 elements inside
		/// If the given <paramref name="text"/> is not in an <seealso cref="Dictionary{TKey, TValue}"/> type format,
		/// it will return an empty dictionary
		/// </summary>
		/// <remarks>
		/// It provides extra custom <paramref name="deserializers"/> to specific parsing
		/// </remarks>
		/// <exception cref="FormatException">
		/// Thrown if the given <paramref name="text"/> is not in the given <typeparamref name="TKey"/> or <typeparamref name="TValue"/> type format
		/// </exception>
		/// <exception cref="IndexOutOfRangeException">
		/// Thrown if the given <paramref name="text"/> has a odd amount of values to pair. Must always be an even amount of values
		/// </exception>
		public static Dictionary<TKey, TValue> DictionaryParse<TKey, TValue>(
		string text, params Func<string, Type, object>[] deserializers)
		{
			var keyType = typeof(TKey);
			var valueType = typeof(TValue);
			return DictionaryParse(text, keyType, valueType, typeof(Dictionary<,>).MakeGenericType(keyType, valueType),
			                       deserializers) as Dictionary<TKey, TValue>;
		}

		/// <inheritdoc cref="DictionaryParse{TKey,TValue}" />
		private static object DictionaryParse(string text, Type keyType, Type valueType, Type dictionaryType,
		                                      params Func<string, Type, object>[] deserializers)
		{
			var items = ArrayParse<string>(text, deserializers);
			var dictionary = (IDictionary) Activator.CreateInstance(dictionaryType);

			if (items[0].IndexOfAny(PairSplitChars) != -1)
			{
				foreach (var item in items)
				{
					var split = item.Split(PairSplitChars);
					var key = Parse(split[0], keyType, deserializers);
					var value = Parse(split[1], valueType, deserializers);

					dictionary.Add(key, value);
				}
			}
			else if (items.Count % 2 == 1)
			{
				throw new
					IndexOutOfRangeException($"Dictionary must have an even amount of values and the following text" +
					                         $"has {items.Count.ToString()} values. \nText:{text}");
			}
			else
			{
				for (var i = 0; i < items.Count; i += 2)
				{
					var key = Parse(items[i], keyType, deserializers);
					var value = Parse(items[i + 1], valueType, deserializers);

					dictionary.Add(key, value);
				}
			}

			return dictionary;
		}

		/// <summary>
		/// Parses the given <paramref name="text"/> to the given <typeparamref name="T"/> type
		/// </summary>
		/// <remarks>
		/// It provides extra custom <paramref name="deserializers"/> to specific parsing
		/// </remarks>
		/// <exception cref="FormatException">
		/// Thrown if the given <paramref name="text"/> is not in the given <typeparamref name="T"/> type format
		/// </exception>
		public static T Parse<T>(string text, params Func<string, Type, object>[] deserializers)
		{
			return (T) Parse(text, typeof(T), deserializers);
		}

		/// <inheritdoc cref="Parse{T}" />
		public static object Parse(string text, Type type, params Func<string, Type, object>[] deserializers)
		{
			text = text.Trim();

			if (type == typeof(string))
			{
				return text;
			}

			if (type.IsEnum)
			{
				return Enum.Parse(type, text);
			}

			if (TryGetKeyValuePair(text, type, out var key, out var value, deserializers))
			{
				return Activator.CreateInstance(type, key, value);
			}

			deserializers ??= Array.Empty<Func<string, Type, object>>();

			foreach (var func in deserializers)
			{
				var res = func(text, type);

				if (res != null)
				{
					return res;
				}
			}

			if (type == typeof(DateTime))
			{
				return DateTime.Parse(text);
			}

			if (type == typeof(TimeSpan))
			{
				return TimeSpan.Parse(text);
			}

			//Handling Nullable types i.e, int?, double?, bool? .. etc
			if (type.IsValueType && Nullable.GetUnderlyingType(type) != null)
			{
				// ReSharper disable once PossibleNullReferenceException
				return TypeDescriptor.GetConverter(type).ConvertFrom(text);
			}

			if (type.IsValueType)
			{
				return Convert.ChangeType(text, type);
			}

			return JsonConvert.DeserializeObject($"\"{text}\"", type);
		}

		private static string[] EnumerateCsvLine(string line)
		{
			// Regex taken from http://wiki.unity3d.com/index.php?title=CSVReader
			const string match = @"(((?<x>(?=[,\r\n]+))|""(?<x>([^""]|"""")+)""|(?<x>[^,\r\n]+)),?)";

			var matches = Regex.Matches(line, match, RegexOptions.ExplicitCapture);
			var ret = new string[matches.Count];

			for (var i = 0; i < matches.Count; i++)
			{
				ret[i] = matches[i].Groups[1].Value;
			}

			return ret;
		}

		private static string SerializedKeyValuePair(string text)
		{
			var split = text.Split(PairSplitChars);

			return $"{{\"Key\":\"{split[0].Trim()}\",\"Value\":\"{split[1].Trim()}\"}}";
		}

		private static bool TryGetKeyValuePair(string data, Type type, out object key, out object value,
		                                       params Func<string, Type, object>[] deserializers)
		{
			if (!type.IsValueType)
			{
				key = null;
				value = null;

				return false;
			}

			var fields = type.GetFields();
			var split = data.Split(PairSplitChars);

			if (type.IsGenericType && fields.Length == 2 && (fields[0].Name == "Key" || fields[0].Name == "Value1") &&
			    (fields[1].Name == "Value" || fields[1].Name == "Value2"))
			{
				key = string.IsNullOrWhiteSpace(data) ? default : Parse(split[0], fields[0].FieldType, deserializers);
				value = string.IsNullOrWhiteSpace(data) ? default : Parse(split[1], fields[1].FieldType, deserializers);

				return true;
			}

			key = null;
			value = null;

			return false;
		}
	}
}