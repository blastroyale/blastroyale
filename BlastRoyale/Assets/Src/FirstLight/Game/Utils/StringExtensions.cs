using System;
using System.Text.RegularExpressions;

namespace Src.FirstLight.Game.Utils
{
	public static class StringExtensions
	{
		/// <summary>
		/// Returns a camel case string like YouAreYolo to a
		/// string of separated words You Are Yolo
		/// </summary>
		public static string CamelCaseToSeparatedWords(this string s)
		{
			return Regex.Replace(s, "(\\B[A-Z])", " $1");
		}
	}
}