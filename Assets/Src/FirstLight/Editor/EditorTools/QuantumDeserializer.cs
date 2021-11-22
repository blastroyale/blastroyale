using System;
using System.Collections.Generic;
using FirstLight.GoogleSheetImporter;
using Photon.Deterministic;
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
			return CsvParser.DeserializeTo<T>(data, FpDeserializer);
		}

		/// <inheritdoc cref="CsvParser.Parse" />
		/// <remarks>
		/// It allows to part quantum types
		/// </remarks>
		public static object FpDeserializer(string data, Type type)
		{
			if (type == typeof(FP))
			{
				return FP.FromString(data);
			}
			
			return null;
		}
	}
}