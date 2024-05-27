using System;
using System.Collections.Generic;
using System.Text;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Services.Party
{
	public class JoinCodeUtils
	{
		public const string AllowedCharacters = "23456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

		private static readonly Dictionary<char, char> CharCodeReplaces = new()
		{
			{'1', 'I'},
			{'0', 'O'}
		};

		public static string GenerateCode(int digits)
		{
			var code = new StringBuilder();
			for (int i = 0; i < digits; i++)
			{
				var rndIndex = Random.Range(0, AllowedCharacters.Length);
				code.Append(AllowedCharacters[rndIndex]);
			}

			return code.ToString();
		}

		public static string NormalizeCode(string code)
		{
			var temp = code.ToUpper();
			foreach (var (from, to) in CharCodeReplaces)
			{
				temp = temp.Replace(from, to);
			}

			return temp;
		}
	}
}