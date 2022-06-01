using System;
using System.Collections.Generic;
using FirstLight.GoogleSheetImporter;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

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
			
			var values = data.Split(CsvParser.PairSplitChars);
			
			if (values.Length > 2)
			{
				throw new
					IndexOutOfRangeException($"Trying to deserialize more than 2 values to a value pair: ({data})");
			}
			
			var genericType = type.GetGenericArguments()[0];
			var value1 = CsvParser.Parse(values[0], genericType, null);
			var value2 = values.Length == 1 ? value1 : CsvParser.Parse(values[1], genericType, null);
			var valuePair = Activator.CreateInstance(gameModePairType.MakeGenericType(genericType));
			var brField = valuePair.GetType().GetField(nameof(QuantumGameModePair<object>.BattleRoyale));
			var dmField = valuePair.GetType().GetField(nameof(QuantumGameModePair<object>.Deathmatch));

			brField.SetValue(valuePair, value1);
			dmField.SetValue(valuePair, value2);

			return valuePair;
		}
	}
}