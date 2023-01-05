using System;
using Cinemachine;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Helper methods for authentication.
	/// </summary>
	public static class AuthenticationUtils
	{
		
		public static bool IsUsernameFieldValid(string username)
		{
			return String.Compare(username, string.Empty, StringComparison.Ordinal) != 0;
		}

		public static bool IsPasswordFieldValid(string password)
		{
			return String.Compare(password, string.Empty, StringComparison.Ordinal) != 0;
		}

		public static bool IsEmailFieldValid(string email)
		{
			var trimmedEmail = email.Trim();

			if (trimmedEmail.EndsWith("."))
			{
				return false;
			}

			try
			{
				var addr = new System.Net.Mail.MailAddress(email);
				return addr.Address == trimmedEmail;
			}
			catch
			{
				return false;
			}
		}
	}
}