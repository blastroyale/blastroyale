using System;
using System.Text;

namespace FirstLightServerSDK.Modules
{
	public static class TypeExtensions
	{
		/// <summary>
		/// Gets the real type name of a type, which include generics
		/// </summary>
		public static string GetRealTypeName(this Type t)
		{
			if (!t.IsGenericType)
				return t.Name;

			StringBuilder sb = new StringBuilder();
			sb.Append(t.Name.Substring(0, t.Name.IndexOf('`')));
			sb.Append('<');
			bool appendComma = false;
			foreach (Type arg in t.GetGenericArguments())
			{
				if (appendComma) sb.Append(',');
				sb.Append(GetRealTypeName(arg));
				appendComma = true;
			}
			sb.Append('>');
			return sb.ToString();
		}
	}
}