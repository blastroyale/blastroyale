using System;
using System.Collections.Generic;
using System.Reflection;
using FirstLight.GoogleSheetImporter;
using Photon.Deterministic;
using Quantum;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// This class has a list of useful extensions to deserialize Quantum Configs
	/// </summary>
	public static class QuantumDeserializer
	{
		/// <inheritdoc cref="CsvParser.DeserializeTo{T}" />
		public static T DeserializeTo<T>(Dictionary<string, string> data)
		{
			return CsvParser.DeserializeTo<T>(data, FpDeserializer, QuantumGameModePairDeserializer);
		}

		/// <inheritdoc cref="CsvParser.Parse" />
		/// <remarks>
		/// It allows to parse quantum types
		/// </remarks>
		public static object FpDeserializer(string data, Type type)
		{
			if (type == typeof(FP))
			{
				return FP.FromString(data);
			}

			return null;
		}

		/// <inheritdoc cref="CsvParser.Parse" />
		/// <remarks>
		/// It allows to parse <see cref="QuantumGameModePair{TValue}"/> types
		/// </remarks>
		public static object QuantumGameModePairDeserializer(string data, Type type)
		{
			var gameModePairType = typeof(QuantumGameModePair<>);

			if (!type.IsGenericType || !gameModePairType.IsAssignableFrom(type.GetGenericTypeDefinition()))
			{
				return null;
			}

			var lines = data.Split(CsvParser.NewLineChars, StringSplitOptions.RemoveEmptyEntries);

			var genericType = type.GetGenericArguments()[0];
			var defaultValue = CsvParser.Parse(lines[0], genericType, FpDeserializer);
			var keys = new List<string>();
			var values = new List<string>();

			for (var i = 1; i < lines.Length; i++)
			{
				var split = lines[i].Split(CsvParser.PairSplitChars);
				keys.Add(split[0]);
				values.Add(split[1]);
			}

			var valuePair = Activator.CreateInstance(gameModePairType.MakeGenericType(genericType));
			var defaultField = valuePair.GetType().GetField(nameof(QuantumGameModePair<object>.Default));
			var keysField = valuePair.GetType().GetField(nameof(QuantumGameModePair<object>.Keys));
			var valuesField = valuePair.GetType().GetField(nameof(QuantumGameModePair<object>.Values));

			defaultField.SetValue(valuePair, defaultValue);
			keysField.SetValue(valuePair, keys);
			valuesField.SetValue(valuePair,
			                     CsvParser.ArrayParse(string.Join(",", values), genericType, FpDeserializer));

			return valuePair;
		}
	}
}