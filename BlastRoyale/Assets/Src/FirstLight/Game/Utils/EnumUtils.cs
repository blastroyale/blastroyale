using System;
using System.Linq;
using Src.FirstLight.Game.Utils;

namespace FirstLight.Game.Utils
{
	public static class EnumUtils
	{
		/// <summary>
		/// Convert an enum value to an integer based on the index
		/// </summary>
		public static int ToInt<T>(T value) where T : Enum
		{
			return Array.IndexOf(Enum.GetValues(typeof(T)), value);
		}

		/// <summary>
		/// Get an enum value based on the index in the allowed list
		/// </summary>
		public static T FromInt<T>(T[] allowedValues, int value) where T : Enum
		{
			return allowedValues[value];
		}

		/// <summary>
		/// Convert an enum value to an integer based on the index
		/// </summary>
		public static int ToInt<T>(T[] allowedValues, T value) where T : Enum
		{
			return Array.IndexOf(allowedValues, value);
		}

		/// <summary>
		/// Return if a index is valid in a given list of options
		/// </summary>
		public static bool IsValid<T>(T[] allowedValues, T value) where T : Enum
		{
			return allowedValues.Contains(value);
		}

		/// <summary>
		/// Get an enum value based on the index
		/// </summary>
		public static T FromInt<T>(int value) where T : Enum
		{
			return (T)Enum.GetValues(typeof(T)).GetValue(value);
		}

		public static string ToStringSeparatedWords(this Enum e)
		{
			return e.ToString().CamelCaseToSeparatedWords();
		}
	}
}