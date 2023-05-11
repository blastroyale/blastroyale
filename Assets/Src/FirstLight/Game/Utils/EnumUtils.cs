using System;

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
		/// Get an enum value based on the index
		/// </summary>
		public static T FromInt<T>(int value) where T : Enum
		{
			return (T)Enum.GetValues(typeof(T)).GetValue(value);
		}
	}
}